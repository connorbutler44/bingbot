using System.Threading.Tasks;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.IO;
using Discord;
using OpenAI.Managers;
using OpenAI;
using OpenAI.ObjectModels.RequestModels;
using System.Linq;
using OpenAI.ObjectModels;

namespace Bingbot.Modules
{
    public class ChatModule : InteractionModuleBase<SocketInteractionContext>
    {
        IServiceProvider _provider;
        ElevenLabsTextToSpeechService _ttsService = new();
        ChatService _chatService;

        public ChatModule(IServiceProvider provider, ChatService chatService)
        {
            _provider = provider;
            _chatService = chatService;
        }

        [SlashCommand("ask", "Ask Bingbot", runMode: RunMode.Async)]
        public async Task AskBingbot(
            [Summary(description: "What you wanna ask :)")]
            string question,
            [Summary(description: "Should the bot respond with audio or text")]
            Boolean respondWithAudio = false
        )
        {
            await RespondAsync("hmm... lemme think about it");

            string response;
            try
            {
                response = await _chatService.askBingbotAsync(Context.User, question);
            }
            catch (Exception)
            {
                await ModifyOriginalResponseAsync(x =>
                {
                    x.Content = "Command failed :-(";
                });
                return;
            }

            var embed = new EmbedBuilder
            {
                Color = new Discord.Color(90, 63, 113),
                Description = $"*{question}*\n\n{response}",
                Author = new EmbedAuthorBuilder
                {
                    Name = Context.User.Username,
                    IconUrl = Context.User.GetAvatarUrl()
                },
                Footer = new EmbedFooterBuilder
                {
                    Text = "Bingbot",
                    IconUrl = "https://cdn.discordapp.com/avatars/980552854500683896/4eb7762a200558b4d86f5f215e7076c6.png"
                }
            };

            if (respondWithAudio)
            {
                // TODO make a new voice for bingbot specifically
                Stream stream = await _ttsService.GetTextToSpeechAsync(response, "S4MX3ES8njsedM3zBZhJ", 35, 85);

                var fileAttachment = new FileAttachment(stream: stream, fileName: "media.mp3");

                await ModifyOriginalResponseAsync(x =>
                {
                    x.Content = " ";
                    x.Embed = embed.Build();
                    x.Attachments = new List<FileAttachment>() { fileAttachment };
                });
            }
            else
            {
                await ModifyOriginalResponseAsync(x =>
                {
                    x.Content = " ";
                    x.Embed = embed.Build();
                });
            }
        }

        [SlashCommand("visualize", "Create an image based on a prompt", runMode: RunMode.Async)]
        public async Task Visualize(
            [Summary(description: "What should the image look like")]
            string prompt
        )
        {
            await RespondAsync("Gathering my art supplies...");
            var openAiService = new OpenAIService(new OpenAiOptions()
            {
                ApiKey = Environment.GetEnvironmentVariable("OPEN_API_KEY")
            });

            var response = await openAiService.CreateImage(new ImageCreateRequest
            {
                Size = StaticValues.ImageStatics.Size.Size1024,
                Prompt = prompt,
                ResponseFormat = "b64_json",
                Model = Models.Dall_e_3
            });

            if (!response.Successful)
            {
                Console.WriteLine("OpenAI image generation failed");
                Console.WriteLine(response.Error.Message);
                await ModifyOriginalResponseAsync(x =>
                {
                    x.Content = "Command failed :-(";
                });
            }

            var result = response.Results.FirstOrDefault();

            var embed = new EmbedBuilder
            {
                Color = new Discord.Color(90, 63, 113),
                Description = $"*{prompt}*",
                Author = new EmbedAuthorBuilder
                {
                    Name = Context.User.Username,
                    IconUrl = Context.User.GetAvatarUrl()
                },
                ImageUrl = "attachment://image.png",
                Footer = new EmbedFooterBuilder
                {
                    Text = "Bingbot",
                    IconUrl = "https://cdn.discordapp.com/avatars/980552854500683896/4eb7762a200558b4d86f5f215e7076c6.png"
                }
            };

            var fileAttachment = new FileAttachment(new MemoryStream(Convert.FromBase64String(result.B64)), "image.png");

            await ModifyOriginalResponseAsync(x =>
            {
                x.Content = " ";
                x.Attachments = new List<FileAttachment> { fileAttachment };
                x.Embed = embed.Build();
            });
        }
    }
}