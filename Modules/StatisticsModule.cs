using System.Threading.Tasks;
using Discord.Interactions;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Discord;
using System.Collections.Generic;

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
        public async Task Log(
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

        [SlashCommand("stats", "Log stats!", runMode: RunMode.Async)]
        public async Task Stats()
        {
            // total number of logs for this user
            var total = (
                await _db.UserLogs
                    .Where(x => x.UserId == Context.User.Id && x.GuildId == Context.Guild.Id)
                    .ToListAsync()
            ).Count;

            // number of logs for this user this week
            var startOfWeek = DateTimeOffset.Now.AddDays(-(int)DateTimeOffset.Now.DayOfWeek + (int)DayOfWeek.Monday).UtcDateTime;

            var thisWeekTotal = (
                await _db.UserLogs
                    .Where(x => x.TakenAt >= startOfWeek && x.UserId == Context.User.Id && x.GuildId == Context.Guild.Id)
                    .ToListAsync()
            ).Count;

            // most logs in a singular day
            var mostInDay = (
                await _db.UserLogs
                    .Where(x => x.UserId == Context.User.Id && x.GuildId == Context.Guild.Id)
                    .GroupBy(x => x.TakenAt.Date)
                    .Select(x => new { Date = x.Key, Count = x.Count() })
                    .OrderByDescending(x => x.Count)
                    .FirstOrDefaultAsync()
            ).Count;

            var embed = new EmbedBuilder
            {
                Color = new Color(191, 105, 82),
                Title = $"{Context.User.Username}'s 💩 stats",
                Fields = new List<EmbedFieldBuilder>
                {
                    new EmbedFieldBuilder
                    {
                        Name = "Total",
                        Value = total,
                        IsInline = true
                    },
                    new EmbedFieldBuilder
                    {
                        Name = "This week",
                        Value = thisWeekTotal,
                        IsInline = true
                    },
                    new EmbedFieldBuilder
                    {
                        Name = "Most in a day",
                        Value = mostInDay,
                        IsInline = true
                    }
                },
            };

            await RespondAsync(embed: embed.Build());
        }
    }
}