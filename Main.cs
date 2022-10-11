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
using Reddit;
using Reddit.Controllers;
using Reddit.Controllers.EventArgs;
using TwitchLib.Api;

namespace Bingbot
{
    class Program
    {
        HttpClient client = new HttpClient();
        private readonly DiscordSocketClient _discordClient;
        private readonly RedditClient _redditClient;
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

            _redditClient = new RedditClient(
                appId: Environment.GetEnvironmentVariable("REDDIT_CLIENT_ID"),
                refreshToken: Environment.GetEnvironmentVariable("REDDIT_REFRESH_TOKEN"),
                appSecret: Environment.GetEnvironmentVariable("REDDIT_CLIENT_SECRET")
            );
            SetupRedditClient();

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

        private void SetupRedditClient()
        {
            var lsf = _redditClient.Subreddit("LivestreamFail");
            // lsf.Posts.GetNew();
            // lsf.Posts.MonitorNew();
            // lsf.Posts.NewUpdated += NewLsfPostsRecieved;
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
                case "bingbot-imagine":
                    await command.RespondAsync("https://cdn.discordapp.com/attachments/688040246499475525/1029238380829093918/unknown.png");
                    break;
            }

        }

        private async void NewLsfPostsRecieved(object sender, PostsUpdateEventArgs e)
        {
            foreach (Post post in e.Added)
            {
                try
                {
                    // no self-posts will be in this queue - safe to assume it's a LinkPost
                    string postUrl = ((LinkPost)post).URL;
                    if (!postUrl.Contains("clips.twitch.tv"))
                    {
                        Console.WriteLine($"Unsupported clip format {postUrl}");
                        return;
                    }

                    // channel to send the update stream to
                    var channel = await _discordClient.GetChannelAsync(DISCORD_CHANNEL_POST_STREAM_ID);

                    // extract the clip id (there are more clip URL formats but generally this format is used - works well enough for the meme)
                    var clipId = Regex.Match(postUrl, @"(?:https:\/\/)?clips\.twitch\.tv\/(\S+)").Groups[1].Value;
                    
                    var clip = (await _twitchClient.Helix.Clips.GetClipsAsync(new List<string>{ clipId })).Clips[0];
                    var streamer = (await _twitchClient.Helix.Users.GetUsersAsync(logins: new List<string>{ clip.BroadcasterName } )).Users[0];

                    if (
                        !postUrl.ToLower().Contains("xqc") &&
                        !post.Title.ToLower().Contains("xqc") &&
                        !clip.Title.ToLower().Contains("xqc") &&
                        !streamer.DisplayName.ToLower().Contains("xqc")
                    )
                    {
                        Console.WriteLine($"Clip isn't relevant {postUrl}, {post.Title}, {clip.Title}, {streamer.DisplayName}");
                        return;
                    }

                    Embed postEmbed = GenerateTwitchClipEmbed(post, clip, streamer);
                    await (channel as ITextChannel).SendMessageAsync(embed: postEmbed);
                }
                catch(Exception err)
                {
                    Console.WriteLine("Error processing post");
                    Console.WriteLine(err);
                    Console.WriteLine($"Problematic post: {post.Permalink}");
                }
            }
        }

        private Embed GenerateTwitchClipEmbed(
            Post post,
            TwitchLib.Api.Helix.Models.Clips.GetClips.Clip clip,
            TwitchLib.Api.Helix.Models.Users.GetUsers.User streamer
        )
        {
            var embed = new EmbedBuilder
                {
                    Title = post.Title,
                    Description = $"[Reddit thread](https://reddit.com{post.Permalink})",
                    Url = clip.Url,
                    Color = new Color(r: 145, g: 70, b: 255),
                    ImageUrl = clip.ThumbnailUrl
                };
            var author = new EmbedAuthorBuilder
                {
                    Name = streamer.DisplayName,
                    Url = $"https://twitch.tv/{streamer.DisplayName}",
                    IconUrl = streamer.ProfileImageUrl
                };
            
            return embed.WithAuthor(author)
                .WithCurrentTimestamp()
                .Build();
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
        }
    }
}
