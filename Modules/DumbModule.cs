using System.Threading.Tasks;
using Discord.Interactions;
using System;
using Discord;

namespace Bingbot.Modules
{
    [Group("give", "Steal an emote from another servew")]
    public class DumbModule : InteractionModuleBase<SocketInteractionContext>
    {
        IServiceProvider _provider;
        ElevenLabsTextToSpeechService _ttsService;

        public DumbModule(IServiceProvider provider, ElevenLabsTextToSpeechService ttsService)
        {
            _provider = provider;
            _ttsService = ttsService;
        }

        [SlashCommand("cookie", "Give Bingbot a Cookie", runMode: RunMode.Async)]
        public async Task GiveCookie()
        {
            await RespondAsync("<:thankyou:1143324621874151575>");
        }

        [SlashCommand("garlic", "Give Garlic", runMode: RunMode.Async)]
        public async Task GiveUserGarlic(IUser? user = null)
        {
            if (user is null)
            {
                await RespondAsync("<:holy:1415754810044580011>");
            }
            else if (user.Id == Context.User.Id)
            {
                await RespondAsync($"{user.Mention} has given garlic to themselves...!");
            }
            else
            {
                await RespondAsync($"{Context.User.Mention} has given garlic to {user.Mention}!");
            }
        }
    }
}