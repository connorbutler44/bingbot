using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using System.Net.Http;
using System.Text.Json;
using TwitchLib.Api;
using System.Linq;

namespace Bingbot
{
    class Program
    {
        HttpClient client = new HttpClient();
        private readonly DiscordSocketClient _discordClient;
        private readonly TwitchAPI _twitchClient;
        private readonly ITextToSpeechService _ttsService;
        private readonly string VOICE_CODES_URL = "https://gist.githubusercontent.com/connorbutler44/118d8c69e42de0113cd629fc5985b625/raw/05373087ebad19bcc3f6ff4f7823942693d7d1e4/bingbot_voice_codes.json";


        private Dictionary<string, string> emoteDictionary = new Dictionary<string, string>();


        // Discord.Net heavily utilizes TAP for async, so we create
        // an asynchronous context from the beginning.
        static void Main(string[] args)
            => new Program()
                .MainAsync()
                .GetAwaiter()
                .GetResult();

        public Program()
        {
            _discordClient = new DiscordSocketClient();
            SetupDiscordClient();

            _twitchClient = new TwitchAPI();
            _twitchClient.Settings.ClientId = Environment.GetEnvironmentVariable("TWITCH_CLIENT_ID");
            _twitchClient.Settings.AccessToken = Environment.GetEnvironmentVariable("TWITCH_CLIENT_ACCESS_TOKEN");

            _ttsService = new ElevenLabsTextToSpeechService();
        }

        public async Task MainAsync()
        {
            string apiKey = Environment.GetEnvironmentVariable("DISCORD_API_KEY");

            await _discordClient.LoginAsync(TokenType.Bot, apiKey);
            await _discordClient.StartAsync();

            // Block the program until it is closed.
            await Task.Delay(Timeout.Infinite);
        }

        private void SetupDiscordClient()
        {
            _discordClient.Log += LogAsync;
            _discordClient.Ready += DiscordClientReadyAsync;
            _discordClient.MessageReceived += MessageReceivedAsync;
            _discordClient.ReactionAdded += ReactionAddedAsync;
            _discordClient.SlashCommandExecuted += SlashCommandHandlerAsync;
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        // The Ready event indicates that the client has opened a
        // connection and it is now safe to access the cache.
        private async Task DiscordClientReadyAsync()
        {
            Console.WriteLine($"{_discordClient.CurrentUser} is connected!");

            await RefreshEmoteDictionary();
            await SetupSlashCommands();
        }

        private async Task SetupSlashCommands()
        {
            List<ApplicationCommandProperties> applicationCommandProperties = new();

            var ttsCommand = new SlashCommandBuilder();
            ttsCommand.WithName("tts");
            ttsCommand.WithDescription("ElevenLabs AI Text to Speech");

            var options = new ApplicationCommandOptionChoiceProperties[] {
                new ApplicationCommandOptionChoiceProperties{ Name = "Obama", Value = "b5EjCnCMCw9XA8W0FFMT" },
                new ApplicationCommandOptionChoiceProperties{ Name = "Arnold", Value = "VR6AewLTigWG4xSOukaG" },
                new ApplicationCommandOptionChoiceProperties{ Name = "Rachel", Value = "21m00Tcm4TlvDq8ikWAM" },
            };

            ttsCommand.AddOption(name: "voice", type: ApplicationCommandOptionType.String, description: "Voice to be used for tts", isRequired: true, choices: options);
            ttsCommand.AddOption(name: "text", type: ApplicationCommandOptionType.String, description: "Text to be used for tts", isRequired: true);
            applicationCommandProperties.Add(ttsCommand.Build());

            try
            {
                await _discordClient.BulkOverwriteGlobalApplicationCommandsAsync(applicationCommandProperties.ToArray());
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        private async Task SlashCommandHandlerAsync(SocketSlashCommand command)
        {
            Console.WriteLine($"Executed command {command.Data.Name}");

            switch (command.Data.Name)
            {
                case "tts":
                    await TextToSpeechCommandHandlerAsync(command);
                    break;
            }

        }

        private async Task TextToSpeechCommandHandlerAsync(SocketSlashCommand command)
        {
            await command.RespondAsync("You got it, boss. Working on it...");

            var voice = (String)command.Data.Options.ElementAt(0).Value;
            var text = (String)command.Data.Options.ElementAt(1).Value;

            string sanitizedInput = Regex.Replace(text, @"<a{0,1}:(\w+):[0-9]+>", "$1");

            Stream stream = await _ttsService.GetTextToSpeechAsync(sanitizedInput, voice);

            var fileAttachment = new FileAttachment(stream: stream, fileName: "media.mp3");

            await command.ModifyOriginalResponseAsync(x =>
            {
                x.Attachments = new List<FileAttachment>() { fileAttachment };
                x.Content = "Here ya go boss";
            });
        }

        private async Task MessageReceivedAsync(SocketMessage message)
        {
            // The bot should never respond to itself.
            if (message.Author.Id == _discordClient.CurrentUser.Id)
                return;

            if (message.Content == "!emoterefresh")
            {
                await RefreshEmoteDictionary();
                await message.Channel.SendMessageAsync("Emote Dictionary Refreshed 👍");
            }
            return;
        }

        private async Task ReactionAddedAsync(Cacheable<IUserMessage, ulong> cachedMessage, Cacheable<IMessageChannel, ulong> originChannel, SocketReaction reaction)
        {
            var message = await cachedMessage.GetOrDownloadAsync();

            if (reaction.Emote.Name == "📣" && message.Content.Trim().Length > 0)
            {
                string sanitizedInput = Regex.Replace(message.Content, @"<a{0,1}:(\w+):[0-9]+>", "$1");
                string voice = GetVoice(message.Reactions.Keys);
                Stream stream = await _ttsService.GetTextToSpeechAsync(sanitizedInput, voice);
                await message.Channel.SendFileAsync(stream: stream, filename: "media.mp3", messageReference: new MessageReference(message.Id));
            }
        }

        private string GetVoice(IEnumerable<IEmote> reactions)
        {
            foreach (IEmote reaction in reactions)
            {
                if (emoteDictionary.ContainsKey(reaction.Name))
                    return emoteDictionary[reaction.Name];
            }

            // fallback to UsFemale if no valid reaction is found
            return Voice.UsFemale;
        }

        /*
            Emote voice mappings are stored in a gist so we can easily update them without committing/redeploying the bot
        */
        private async Task RefreshEmoteDictionary()
        {
            Console.WriteLine("Refreshing Emote Dictionary");

            Stream response = await client.GetStreamAsync(VOICE_CODES_URL);
            var voiceCodes = JsonSerializer.Deserialize<List<VoiceCode>>(response);

            var emoteDict = new Dictionary<string, string>();

            foreach (var voiceCode in voiceCodes)
            {
                emoteDict.Add(voiceCode.emote, voiceCode.code);
            }

            this.emoteDictionary = emoteDict;
            return;
        }
    }
}
