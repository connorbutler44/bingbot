using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using System.Net.Http;

namespace Bingbot
{
    class Program
    {
        HttpClient client = new HttpClient();
        private readonly DiscordSocketClient _discordClient;
        private readonly ITextToSpeechService _ttsService;


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
                new ApplicationCommandOptionChoiceProperties{ Name = "Jarrad", Value = "iKfvwsmhoDNziMjUHyUo" },
                new ApplicationCommandOptionChoiceProperties{ Name = "Asher", Value = "QBaMLEOzUYxyKKh7ZuZN" },
                new ApplicationCommandOptionChoiceProperties{ Name = "Todd Howard", Value = "8oLT3oOTUp9RV6GHl7AU" },
                new ApplicationCommandOptionChoiceProperties{ Name = "Whopper", Value = "Zuzo46BJSET6252mCZX5" },
                new ApplicationCommandOptionChoiceProperties{ Name = "Tim Gunn", Value = "lQV6YBaetZO5fb2n1JSV" },
                new ApplicationCommandOptionChoiceProperties{ Name = "Halo Announcer", Value = "2mY0k5zCDvLApJhuUvS4" },
                new ApplicationCommandOptionChoiceProperties{ Name = "Cortana", Value = "G5JvFs8Ivxbn4PsafONN" },
                new ApplicationCommandOptionChoiceProperties{ Name = "Chills", Value = "XmxA2cgmSD8ydzjAjP7G" },
            };

            ttsCommand.AddOption(
                name: "voice",
                type: ApplicationCommandOptionType.String,
                description: "Voice to be used for tts",
                isRequired: true,
                choices: options);
            ttsCommand.AddOption(
                name: "text",
                type: ApplicationCommandOptionType.String,
                description: "Text to be used for tts",
                isRequired: true);
            ttsCommand.AddOption(
                name: "stability",
                type: ApplicationCommandOptionType.Integer,
                description: "0-100. Higher values: consistency + monotonality. Lower values: More expressive + instability.",
                minValue: 0,
                maxValue: 100,
                isRequired: false);
            ttsCommand.AddOption(
                name: "clarity",
                type: ApplicationCommandOptionType.Integer,
                description: "0-100. Higher values for better clarity but could cause artifacting. Lower if artifacts is present.",
                minValue: 0,
                maxValue: 100,
                isRequired: false);

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

            string voice = null, text = null;
            float? stabilty = null, clarity = null;

            foreach (var option in command.Data.Options)
            {
                switch (option.Name)
                {
                    case "voice":
                        voice = (String)option.Value;
                        break;
                    case "text":
                        text = (String)option.Value;
                        break;
                    case "stability":
                        stabilty = ((float)(long)option.Value) / 100;
                        break;
                    case "clarity":
                        clarity = ((float)(long)option.Value) / 100;
                        break;
                    default:
                        break;
                }
            }

            string sanitizedInput = Regex.Replace(text, @"<a{0,1}:(\w+):[0-9]+>", "$1");

            Stream stream = await _ttsService.GetTextToSpeechAsync(sanitizedInput, voice, stabilty, clarity);

            var fileAttachment = new FileAttachment(stream: stream, fileName: "media.mp3");

            await command.ModifyOriginalResponseAsync(x =>
            {
                x.Attachments = new List<FileAttachment>() { fileAttachment };
                x.Content = "Here ya go boss";
            });
        }

        private Task MessageReceivedAsync(SocketMessage message)
        {
            // The bot should never respond to itself.
            if (message.Author.Id == _discordClient.CurrentUser.Id)
                return Task.CompletedTask;
            return Task.CompletedTask;
        }

        private async Task ReactionAddedAsync(Cacheable<IUserMessage, ulong> cachedMessage, Cacheable<IMessageChannel, ulong> originChannel, SocketReaction reaction)
        {
            var message = await cachedMessage.GetOrDownloadAsync();
        }
    }
}
