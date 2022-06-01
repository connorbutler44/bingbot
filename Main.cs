using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;

namespace Bingbot
{
    class Program
    {
        private readonly DiscordSocketClient _client;
        private readonly TextToSpeechService _ttsService;
        private readonly ulong DM_CHANNEL_ID = 688040246499475525;


        // Discord.Net heavily utilizes TAP for async, so we create
        // an asynchronous context from the beginning.
        static void Main(string[] args)
            => new Program()
                .MainAsync()
                .GetAwaiter()
                .GetResult();

        public Program()
        {
            _client = new DiscordSocketClient();

            // Subscribing to client events, so that we may receive them whenever they're invoked.
            _client.Log += LogAsync;
            _client.Ready += ReadyAsync;
            _client.MessageReceived += MessageReceivedAsync;
            _client.ReactionAdded += ReactionAddedAsync;

            _ttsService = new TextToSpeechService();
        }

        public async Task MainAsync()
        {
            JObject env = JObject.Parse(File.ReadAllText(@"./.env.json"));

            // Tokens should be considered secret data, and never hard-coded.
            await _client.LoginAsync(TokenType.Bot, env.GetValue("API_KEY").ToString());
            // Different approaches to making your token a secret is by putting them in local .json, .yaml, .xml or .txt files, then reading them on startup.

            await _client.StartAsync();

            // Block the program until it is closed.
            await Task.Delay(Timeout.Infinite);
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        // The Ready event indicates that the client has opened a
        // connection and it is now safe to access the cache.
        private Task ReadyAsync()
        {
            Console.WriteLine($"{_client.CurrentUser} is connected!");

            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(SocketMessage message)
        {
            // The bot should never respond to itself.
            if (message.Author.Id == _client.CurrentUser.Id)
                return;

            // any DM's the bot recieves will send the TTS to a specific channel
            if (message.Channel.GetChannelType() == ChannelType.DM && message.Content.Trim().Length > 0)
            {
                var fbpChannel = await _client.GetChannelAsync(DM_CHANNEL_ID);
                
                var stream = await _ttsService.GetTextToSpeechAsync(message.Content, Voice.UsFemale);
                await (fbpChannel as ITextChannel).SendFileAsync(stream: stream, filename: "media.mp3");
            }
            return;
        }

        private async Task ReactionAddedAsync(Cacheable<IUserMessage, ulong> cachedMessage, Cacheable<IMessageChannel, ulong> originChannel, SocketReaction reaction)
        {
            var message = await cachedMessage.GetOrDownloadAsync();

            if (reaction.Emote.Name == "📣" && message.Content.Trim().Length > 0)
            {
                var voice = GetVoice(message.Reactions.Keys);
                var stream = await _ttsService.GetTextToSpeechAsync(message.Content, voice);
                await message.Channel.SendFileAsync(stream: stream, filename: "media.mp3", messageReference: new MessageReference(message.Id));
            }
        }

        private static readonly Dictionary<string, string> emoteDictionary = new Dictionary<string, string>()
            {
                { "🐻", Voice.Chewbacca },
                { "🤖", Voice.C3PO },
                { "🦝", Voice.Rocket },
                { "jarrad", Voice.AusMale },
                { "a", Voice.UkMale },
                { "👩", Voice.UsFemale },
                { "BOGGED", Voice.FrenchMale },
                { "🇩🇪", Voice.GermanMale },
                { "c", Voice.SpanishMale },
                { "d", Voice.JpFemale },
                { "e", Voice.KoreanMale },
            };

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
    }
}
