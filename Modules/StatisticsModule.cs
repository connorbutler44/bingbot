using System.Threading.Tasks;
using Discord.Interactions;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Discord;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

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

            var startOfWeek = GetStartOfWeek();

            // get all logs for this week, grouped by user
            var userCountsThisWeek = await _db.UserLogs
                .Where(x => x.TakenAt >= startOfWeek && x.GuildId == Context.Guild.Id)
                .GroupBy(x => x.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            // total number of logs for this user this week
            var thisWeekTotal = userCountsThisWeek.FirstOrDefault(x => x.UserId == Context.User.Id)?.Count ?? 0;
            // rank of this user this week amongst others in this guild
            var currentUserRank = userCountsThisWeek.FindIndex(x => x.UserId == Context.User.Id);

            // most logs in a singular day
            var mostInDay = (
                await _db.UserLogs
                    .Where(x => x.UserId == Context.User.Id && x.GuildId == Context.Guild.Id)
                    .GroupBy(x => x.TakenAt.Date)
                    .Select(x => new { Date = x.Key, Count = x.Count() })
                    .OrderByDescending(x => x.Count)
                    .FirstOrDefaultAsync()
            )?.Count ?? 0;

            var rankingText = currentUserRank switch
            {
                -1 => "n/a",
                0 => "👑",
                _ => $"#{currentUserRank + 1}"
            };

            var embed = new EmbedBuilder
            {
                Color = new Color(191, 105, 82),
                Title = $"{Context.User.Username}'s 💩 stats",
                Fields = new List<EmbedFieldBuilder>
                {
                    new EmbedFieldBuilder
                    {
                        Name = "This week rank",
                        Value = rankingText,
                        IsInline = true
                    },
                    new EmbedFieldBuilder
                    {
                        Name = "This week total",
                        Value = thisWeekTotal,
                        IsInline = true
                    },
                    new EmbedFieldBuilder
                    {
                        Name = "\u200b",
                        Value = "\u200b",
                        IsInline = false
                    },
                    new EmbedFieldBuilder
                    {
                        Name = "Most in a day (lifetime)",
                        Value = mostInDay,
                        IsInline = true
                    },
                    new EmbedFieldBuilder
                    {
                        Name = "Total (lifetime)",
                        Value = total,
                        IsInline = true
                    },
                },
            };

            await RespondAsync(embed: embed.Build());
        }

        [SlashCommand("leaderboard", "💩 weekly leaderboard", runMode: RunMode.Async)]
        public async Task Leaderboard()
        {
            var startOfWeek = GetStartOfWeek();

            var userCountsThisWeek = await _db.UserLogs
                .Where(x => x.TakenAt >= startOfWeek && x.GuildId == Context.Guild.Id)
                .GroupBy(x => x.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            var leaderboard = new List<string>();

            for (int i = 0; i < userCountsThisWeek.Count; i++)
            {
                var user = Context.Guild.GetUser(userCountsThisWeek[i].UserId);
                if (user == null) continue;

                var placementText = i switch
                {
                    0 => "👑",
                    _ => $"{i + 1}."
                };

                leaderboard.Add($"{placementText} **{user.Username}** - {userCountsThisWeek[i].Count}");
            }

            if (leaderboard.Count == 0)
            {
                await RespondAsync("No logs this week!");
            }


            var embed = new EmbedBuilder
            {
                Color = new Color(191, 105, 82),
                Title = $"💩 weekly leaderboard",
                Description = string.Join("\n", leaderboard),
            };

            await RespondAsync(embed: embed.Build());
        }



        private DateTimeOffset GetStartOfWeek()
        {
            return DateTimeOffset.Now.AddDays(-(int)DateTimeOffset.Now.DayOfWeek).UtcDateTime.Date;
        }
    }
}