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
        public async Task GiveCookie()
        {
            await RespondAsync("<:thankyou:1143324621874151575>");
        }

        [SlashCommand("givegarlic", "Give Bingbot Garlic", runMode: RunMode.Async)]
        public async Task GiveGarlic()
        {
            await RespondAsync("<:holy:1415754810044580011>");
        }
    }
}