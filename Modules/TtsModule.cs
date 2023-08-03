using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using System;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Generic;

namespace Bingbot.Modules
{
    public class TtsModule : InteractionModuleBase<SocketInteractionContext>
    {
        IServiceProvider _provider;
        ElevenLabsTextToSpeechService _ttsService = new();

        public TtsModule(IServiceProvider provider)
        {
            _provider = provider;
        }

        [SlashCommand("tts", "ElevenLabs AI Text to Speech", runMode: RunMode.Async)]
        public async Task TextToSpeech(
            [Summary(description: "Voice to be used for tts")]
            [Choice("Obama", "b5EjCnCMCw9XA8W0FFMT"), Choice("Jarrad", "iKfvwsmhoDNziMjUHyUo"),
                Choice("Asher", "QBaMLEOzUYxyKKh7ZuZN"), Choice("Todd Howard", "8oLT3oOTUp9RV6GHl7AU"),
                Choice("Whopper", "Zuzo46BJSET6252mCZX5"), Choice("Tim Gunn", "lQV6YBaetZO5fb2n1JSV"),
                Choice("Halo Announcer", "2mY0k5zCDvLApJhuUvS4"), Choice("Cortana", "G5JvFs8Ivxbn4PsafONN"),
                Choice("Chills", "XmxA2cgmSD8ydzjAjP7G"), Choice("Oblivion Guard", "S4MX3ES8njsedM3zBZhJ"),
                Choice("Girly", "EHEP5eqnatUfacCbMlOR"), Choice("Richard Mealey", "KRHlgM8XFdAaiKgbyzx9"),
                Choice("Deep Voice British Mans", "4OBaNu67U8XfuTFNHrsm")] string voice,
            [Summary(description: "Text to be used for tts")]
            string text,
            [Summary(description: "0-100. Higher values: consistency + monotonality. Lower values: More expressive + instability.")]
            int? stability = null,
            [Summary(description: "0-100. Higher values for better clarity but could cause artifacting. Lower if artifacts is present.")]
            int? clarity = null)
        {
            await RespondAsync("You got it, boss. Working on it...");

            Stream stream = await _ttsService.GetTextToSpeechAsync(SanitizeInput(text), voice, stability, clarity);

            var fileAttachment = new FileAttachment(stream: stream, fileName: "media.mp3");

            await ModifyOriginalResponseAsync(x =>
            {
                x.Attachments = new List<FileAttachment>() { fileAttachment };
                x.Content = " ";
            });
        }

        private string SanitizeInput(string input)
        {
            return Regex.Replace(input, @"<a{0,1}:(\w+):[0-9]+>", "$1");
        }
    }
}