using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CliWrap;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace Bingbot
{
    public class MessageHandler
    {
        private IServiceProvider _serviceProvider;
        private readonly string[] _embedDomainWhitelist = { "reddit.com" };
        public MessageHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task ProcessMessage(IMessage message)
        {
            // find any links in the message content
            MatchCollection urlMatches = Regex.Matches(message.Content, @"\b(?:https?:\/\/|www\.)\S+\b");

            if (
                message.Channel.Id == 905417457232142366 &&
                message.Author.Id == 310249048844271628 &&
                message.Author.ActiveClients.Any(p => p.ToString() == "Mobile"))
            {
                var emote = Emote.Parse("<:ICANT:1143297524396990595>");
                await message.AddReactionAsync(emote);
            }

            if (
                message.Author.Id == 222553872252534786 &&
                message.Content.Contains("🅱️"))
            {
                await message.DeleteAsync();
            }

            foreach (Match match in urlMatches)
            {
                // check if the provided link is in our whitelist to process
                if (Uri.TryCreate(match.Value, UriKind.Absolute, out var uri) && _embedDomainWhitelist.Contains(uri.Host.Replace("www.", "")))
                {
                    await HandleUrlInMessage(uri, message);
                }
            }
        }

        public async Task HandleUrlInMessage(Uri uri, IMessage message)
        {
            try
            {
                var uriBuilder = new UriBuilder(uri);
                uriBuilder.Query = "";
                // .json extension will give us the raw data to see if there's a video associated with the post
                uriBuilder.Path = uriBuilder.Path + ".json";

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/117.0");

                var response = await httpClient.GetAsync(uriBuilder.Uri);

                response.EnsureSuccessStatusCode();

                string jsonContent = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<IEnumerable<RedditPost>>(jsonContent);

                var post = data.First().Data.Children.First().Data;

                if (post.Post_Hint == "hosted:video")
                {
                    // embed new video
                    await EmbedRedditVideo(post, message);
                    // remove the original embed
                    await (message as SocketUserMessage).ModifyAsync(m =>
                    {
                        m.Flags = MessageFlags.SuppressEmbeds;
                    });
                }
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private async Task EmbedRedditVideo(RedditPostDataChildrenData post, IMessage message)
        {
            var tempFilename = Guid.NewGuid() + ".mp4";

            // Hls_Url contains the m3u8 playlist for the audio. Fallback_Url is for the video
            // use ffmpeg to combine the two sources into one output
            await Cli.Wrap("ffmpeg")
                .WithArguments(new[] { "-hide_banner", "-nostats", "-loglevel", "error", "-i", post.Media.Reddit_Video.Hls_Url, "-i", post.Media.Reddit_Video.Fallback_Url, "-c:v", "copy", "-c:a", "aac", "-strict", "experimental", "-map", "1:v:0", "-map", "0:a:0", tempFilename })
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync();

            var fileAttachment = new FileAttachment(tempFilename, fileName: "media.mp4");

            await message.Channel.SendFileAsync(attachment: fileAttachment, messageReference: new MessageReference(message.Id));

            // cleanup file
            if (File.Exists(tempFilename))
            {
                File.Delete(tempFilename);
            }
        }
    }
}