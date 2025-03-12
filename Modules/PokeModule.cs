using System.Threading.Tasks;
using Discord.Interactions;
using Discord;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bingbot.Modules
{
    [Group("poke", "teehee")]
    public class PokeModule : InteractionModuleBase<SocketInteractionContext>
    {

        DataContext _db;
        public PokeModule(DataContext db)
        {
            _db = db;
        }

        [SlashCommand("user", "poke a friend!", runMode: RunMode.Async)]
        public async Task PokeUser(
            [Summary(description: "User you want to poke")]
            IUser user
        )
        {
            await DeferAsync(ephemeral: true);

            await SendPokeMessage(user);
        }

        [ComponentInteraction("poke_back:*", ignoreGroupNames: true)]
        public async Task HandlePokeBack(string userId)
        {
            await DeferAsync(ephemeral: true);

            var user = await Context.Client.GetUserAsync(ulong.Parse(userId));

            await SendPokeMessage(user);
        }

        [SlashCommand("stats", "your poke stats", runMode: RunMode.Async)]
        public async Task PokeStats(
            [Summary(description: "Include stats about your pokes with a specific user")]
            #nullable enable
            IUser? withUser = null)
        {
            await DeferAsync(ephemeral: true);

            if (withUser != null)
            {
                var pokesSent = await _db.Pokes.CountAsync(x => x.SenderId == Context.User.Id && x.RecipientId == withUser.Id);
                var pokesReceived = await _db.Pokes.CountAsync(x => x.SenderId == withUser.Id && x.RecipientId == Context.User.Id);

                var component = new ComponentBuilder()
                    .WithButton("Poke them now", $"poke_back:{withUser.Id}", ButtonStyle.Primary)
                    .Build();

                await ModifyOriginalResponseAsync(properties =>
                {
                    properties.Content = $"You've poked {withUser.Mention} {pokesSent} times\nYou've been poked by {withUser.Mention} {pokesReceived} times";
                    properties.Components = component;
                });
            }
            else
            {
                var pokesSent = await _db.Pokes.CountAsync(x => x.SenderId == Context.User.Id);
                var pokesReceived = await _db.Pokes.CountAsync(x => x.RecipientId == Context.User.Id);

                await ModifyOriginalResponseAsync(properties => properties.Content = $"You've poked {pokesSent} times\nYou've been poked {pokesReceived} times");
            }

        }

        [ComponentInteraction("poke-opt-out", ignoreGroupNames: true)]
        public async Task OptOutButton()
        {
            await DeferAsync(ephemeral: true);

            await PerformOptOut(true);
        }

        [SlashCommand("opt-out", "do you want to be able to send/receive pokes", runMode: RunMode.Async)]
        public async Task OptOut(
            [Summary(description: "Whether or not you want to be able to send/receive pokes")]
            bool optOut = true)
        {
            await DeferAsync(ephemeral: true);

            await PerformOptOut(optOut);
        }

        private async Task PerformOptOut(bool optOut)
        {
            var userSetting = await _db.UserSettings
                .FirstOrDefaultAsync(x => x.Id == Context.User.Id);

            if (userSetting == null)
            {
                userSetting = new UserSetting
                {
                    Id = Context.User.Id,
                    PokesEnabled = !optOut
                };

                await _db.UserSettings.AddAsync(userSetting);
            }
            else
            {
                userSetting.PokesEnabled = !optOut;
            }

            await _db.SaveChangesAsync();

            var actionText = optOut ? "opted out of" : "opted back into";

            await ModifyOriginalResponseAsync(properties =>
            {
                properties.Content = $"Successfully {actionText} pokes";
                properties.Components = null;
            });
        }

        private async Task SendPokeMessage(IUser toUser)
        {
            // #region VALIDATION
            if (toUser.IsBot)
            {
                await ModifyOriginalResponseAsync(properties =>
                {
                    properties.Content = "Don't touch that!";
                    properties.Components = null;
                });
                return;
            }

            if (toUser.Id == Context.User.Id)
            {
                await ModifyOriginalResponseAsync(properties =>
                {
                    properties.Content = "Don't poke yourself!";
                    properties.Components = null;
                });
                return;
            }

            var recipientSettings = await _db.UserSettings
                .FirstOrDefaultAsync(x => x.Id == toUser.Id);

            if (recipientSettings != null && recipientSettings.PokesEnabled == false)
            {
                await ModifyOriginalResponseAsync(properties =>
                {
                    properties.Content = "User doesn't want to be poked 😁";
                    properties.Components = null;
                });
                return;
            }

            var senderSettings = await _db.UserSettings
                .FirstOrDefaultAsync(x => x.Id == Context.User.Id);

            if (senderSettings != null && senderSettings.PokesEnabled == false)
            {
                await ModifyOriginalResponseAsync(properties =>
                {
                    properties.Content = "You must have pokes enabled to poke someone else 🤨";
                    properties.Components = null;
                });
                return;
            }

            // get the last time this user was poked by the current user
            var lastPoke = await _db.Pokes
                .Where(x => x.SenderId == Context.User.Id && x.RecipientId == toUser.Id)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            if (lastPoke != null && lastPoke.CreatedAt > DateTime.UtcNow.AddMinutes(-60))
            {
                await ModifyOriginalResponseAsync(properties =>
                {
                    properties.Content = "Calm down, partner! Wait a little while before poking them again. Don't want to give 'em a bruise";
                    properties.Components = null;
                });
                return;
            }
            // #endregion VALIDATION

            var component = new ComponentBuilder()
                .WithButton("Poke back", $"poke_back:{toUser.Id}", ButtonStyle.Primary)
                .WithButton("Opt Out", "poke-opt-out", ButtonStyle.Secondary)
                .Build();

            await toUser.SendMessageAsync($"You've been poked by {Context.User.Mention}!", components: component);

            await ModifyOriginalResponseAsync(properties =>
            {
                properties.Content = $"You poked {toUser.Mention}!";
                properties.Components = null;
            });

            await _db.Pokes.AddAsync(new Poke
            {
                SenderId = Context.User.Id,
                RecipientId = toUser.Id,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
        }
    }
}