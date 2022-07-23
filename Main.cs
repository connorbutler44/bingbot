using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Text.Json;

namespace Bingbot
{
    class Program
    {
        private readonly DiscordSocketClient _client;
        private readonly TextToSpeechService _ttsService;
        private readonly ulong DM_CHANNEL_ID = 688040246499475525;
        private readonly string VOICE_CODES_URL = "https://gist.githubusercontent.com/connorbutler44/118d8c69e42de0113cd629fc5985b625/raw/e09f33b52829a75286aa76ba0101718717c2949e/gistfile1.txt";

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

            await _client.LoginAsync(TokenType.Bot, env.GetValue("API_KEY").ToString());
            await _client.StartAsync();
            await RefreshEmoteDictionary();

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

            if (message.Content == "!emoterefresh")
            {
                await RefreshEmoteDictionary();
                await message.Channel.SendMessageAsync("Done 👍");
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

            HttpClient client = new HttpClient();
            Stream response = await client.GetStreamAsync(VOICE_CODES_URL);
            var voiceCodes = JsonSerializer.Deserialize<List<VoiceCode>>(response);

            var emoteDict = new Dictionary<string, string>();

            foreach(var voiceCode in voiceCodes)
            {
                emoteDict.Add(voiceCode.emote, voiceCode.code);
            }

            this.emoteDictionary = emoteDict;
        }
    }
}
