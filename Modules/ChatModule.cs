using System.Threading.Tasks;
using Discord.Interactions;
using System;
using OpenAI.GPT3.Managers;
using OpenAI.GPT3;
using OpenAI.GPT3.ObjectModels.RequestModels;
using System.Collections.Generic;
using OpenAI.GPT3.ObjectModels;
using System.Linq;
using System.IO;
using Discord;

namespace Bingbot.Modules
{
    public class ChatModule : InteractionModuleBase<SocketInteractionContext>
    {
        IServiceProvider _provider;
        ElevenLabsTextToSpeechService _ttsService = new();

        public ChatModule(IServiceProvider provider)
        {
            _provider = provider;
        }

        [SlashCommand("ask", "Ask Bingbot", runMode: RunMode.Async)]
        public async Task TextToSpeech(
            [Summary(description: "What you wanna ask :)")]
            string question,
            [Summary(description: "Should the bot respond with audio or text")]
            Boolean respondWithAudio = false
        )
        {
            await RespondAsync("hmm... lemme think about it");

            var openAiService = new OpenAIService(new OpenAiOptions()
            {
                ApiKey = Environment.GetEnvironmentVariable("OPEN_API_KEY")
            });

            var completionResult = await openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
            {
                Messages = new List<ChatMessage>
                {
                    ChatMessage.FromSystem("You are a helpful assistant. Your name is Bingbot"),
                    ChatMessage.FromSystem($"The users name is {Context.User.Username}"),
                    ChatMessage.FromSystem($"Responses should be concise"),
                    ChatMessage.FromUser(question)
                },
                Model = Models.ChatGpt3_5Turbo,
                MaxTokens = 200
            });

            if (!completionResult.Successful)
            {
                Console.WriteLine("OpenAI chat completion failed");
                Console.WriteLine(completionResult.Error.Message);
                await ModifyOriginalResponseAsync(x =>
                {
                    x.Content = "Command failed :(";
                });
            }

            string response = completionResult.Choices.First().Message.Content;

            if (respondWithAudio)
            {
                // TODO make a new voice for bingbot specifically
                Stream stream = await _ttsService.GetTextToSpeechAsync(response, "S4MX3ES8njsedM3zBZhJ", 35, 85);

                var fileAttachment = new FileAttachment(stream: stream, fileName: "media.mp3");

                await ModifyOriginalResponseAsync(x =>
                {
                    x.Attachments = new List<FileAttachment>() { fileAttachment };
                    x.Content = "Here's what I think";
                });
            }
            else
            {
                await ModifyOriginalResponseAsync(x =>
                {
                    x.Content = response;
                });
            }
        }
    }
}