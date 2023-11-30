using System.Threading.Tasks;
using Discord.Interactions;
using System;

namespace Bingbot.Modules
{
    [EnabledInDm(false)]
    [Group("shit", "shit")]
    public class StatisticsModule : InteractionModuleBase<SocketInteractionContext>
    {
        IServiceProvider _provider;
        DataContext _db;

        public StatisticsModule(IServiceProvider provider, DataContext db)
        {
            _provider = provider;
            _db = db;
        }

        [SlashCommand("log", "Dropped a log", runMode: RunMode.Async)]
        public async Task TextToSpeech(
            [Summary(description: "The shit will be logged (haha), but Bingbot won't respond in a way that's public to the server")]
            bool suppressResponse = false)
        {
            await _db.UserLogs.AddAsync(new UserLog
            {
                UserId = Context.User.Id,
                GuildId = Context.Guild.Id,
                TakenAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            await RespondAsync("<:shittin:1135740341182529747>", ephemeral: suppressResponse);
        }
    }
}