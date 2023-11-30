using System.Threading.Tasks;
using Discord.Interactions;
using System;

namespace Bingbot.Modules
{
    public class DumbModule : InteractionModuleBase<SocketInteractionContext>
    {
        IServiceProvider _provider;
        ElevenLabsTextToSpeechService _ttsService;

        public DumbModule(IServiceProvider provider, ElevenLabsTextToSpeechService ttsService)
        {
            _provider = provider;
            _ttsService = ttsService;
        }

        [SlashCommand("givecookie", "Give Bingbot a Cookie", runMode: RunMode.Async)]
        public async Task TextToSpeech()
        {
            await RespondAsync("<:thankyou:1143324621874151575>");
        }
    }
}