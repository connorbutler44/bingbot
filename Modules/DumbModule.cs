using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using System;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Generic;

namespace Bingbot.Modules
{
    public class DumbModule : InteractionModuleBase<SocketInteractionContext>
    {
        IServiceProvider _provider;
        ElevenLabsTextToSpeechService _ttsService = new();

        public DumbModule(IServiceProvider provider)
        {
            _provider = provider;
        }

        [SlashCommand("givecookie", "Give Bingbot a Cookie", runMode: RunMode.Async)]
        public async Task TextToSpeech()
        {
            await RespondAsync("<:thankyou:1143324621874151575>");
        }
    }
}