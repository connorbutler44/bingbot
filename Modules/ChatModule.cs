using System.Threading.Tasks;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.IO;
using Discord;

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
        public async Task TextToSpeech(
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