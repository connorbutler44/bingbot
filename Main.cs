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

namespace Bingbot
{
    class Program
    {
        HttpClient client = new HttpClient();
        private readonly DiscordSocketClient _discordClient;
        private readonly TwitchAPI _twitchClient;
        private readonly TextToSpeechService _ttsService;
        private readonly ulong DISCORD_CHANNEL_POST_STREAM_ID = 937843457039429642;
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

            _ttsService = new TextToSpeechService();
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

            var dramaCommand = new SlashCommandBuilder();
            dramaCommand.WithName("drama-update");
            dramaCommand.WithDescription("Drama update :)");
            applicationCommandProperties.Add(dramaCommand.Build());

            var imagineCommand = new SlashCommandBuilder();
            imagineCommand.WithName("imagine");
            imagineCommand.WithDescription("There are endless possibilities...");
            imagineCommand.AddOption("prompt", ApplicationCommandOptionType.String, "The prompt to imagine", isRequired: true);
            applicationCommandProperties.Add(imagineCommand.Build());

            try {
                await _discordClient.BulkOverwriteGlobalApplicationCommandsAsync(applicationCommandProperties.ToArray());
            }
            catch(Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        private async Task SlashCommandHandlerAsync(SocketSlashCommand command)
        {
            Console.WriteLine($"Executed command {command.Data.Name}");

            switch (command.Data.Name)
            {
                case "drama-update":
                    await command.RespondAsync("🖕");
                    break;
                case "imagine":
                    await command.RespondAsync("https://cdn.discordapp.com/attachments/688040246499475525/1029238380829093918/unknown.png");
                    break;
            }

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

            // any DM's the bot recieves will send the TTS to a specific channel
            // if (message.Channel.GetChannelType() == ChannelType.DM && message.Content.Trim().Length > 0)
            // {
            //     var fbpChannel = await _client.GetChannelAsync(DM_CHANNEL_ID);
                
            //     var stream = await _ttsService.GetTextToSpeechAsync(message.Content, Voice.UsFemale);
            //     await (fbpChannel as ITextChannel).SendFileAsync(stream: stream, filename: "media.mp3");
            // }
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
            foreach(IEmote reaction in reactions)
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

            foreach(var voiceCode in voiceCodes)
            {
                emoteDict.Add(voiceCode.emote, voiceCode.code);
            }

            this.emoteDictionary = emoteDict;
            return;
        }
    }
}
