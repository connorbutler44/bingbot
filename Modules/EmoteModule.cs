using System.Threading.Tasks;
using Discord.Interactions;
using System;
using Discord;
using System.Net.Http;
using System.Collections.Generic;

namespace Bingbot.Modules
{
    [EnabledInDm(false)]
    [DefaultMemberPermissions(GuildPermission.ManageEmojisAndStickers)]
    [Group("yoink", "Steal an emote from another servew")]
    public class EmoteModule : InteractionModuleBase<SocketInteractionContext>
    {
        IServiceProvider _provider;
        private readonly HttpClient client = new HttpClient();

        private readonly List<DomainSubdomainPair> allowedCdns = new List<DomainSubdomainPair>
        {
            new DomainSubdomainPair("cdn", "discordapp.com"),
            new DomainSubdomainPair("cdn", "7tv.app")
        };

        public EmoteModule(IServiceProvider provider)
        {
            _provider = provider;
        }

        [SlashCommand("emote", "steal an emote that already exists on discord", runMode: RunMode.Async)]
        public async Task StealEmote(
            [Summary(description: "Emote you wanna yoink")]
            string emote,
            [Summary(description: "Optional name you want to give it. If not provided, will use the same name as the original emote")]
            string name = null
        )
        {
            if (!Emote.TryParse(emote, out var parsedEmote))
            {
                await RespondAsync("Failed to parse input");
                return;
            }

            // make sure we're downloading from an allowed CDN
            if (!UrlUtil.UrlMatchesDomainAndSubdomain(parsedEmote.Url, allowedCdns))
            {
                await RespondAsync("Invalid Url or domain/subdomain isn't allowed homie");
                return;
            }

            // fetch the image
            var response = await client.GetAsync(parsedEmote.Url);

            if (!response.IsSuccessStatusCode)
            {
                await RespondAsync("Failed to download image");
                return;
            }

            var image = new Image(await response.Content.ReadAsStreamAsync());

            // upload the emote
            var createdEmote = await Context.Guild.CreateEmoteAsync(name ?? parsedEmote.Name, image);

            await RespondAsync($"Yoinked {createdEmote}");
        }

        [SlashCommand("url", "add an emote directly via. url (discord, 7tv, etc.)", runMode: RunMode.Async)]
        public async Task StealUrl(
            [Summary(description: "Url you want to upload")]
            string url,
            [Summary(description: "Name of the emote")]
            string name
        )
        {
            // make sure we're downloading from an allowed CDN
            if (!UrlUtil.UrlMatchesDomainAndSubdomain(url, allowedCdns))
            {
                await RespondAsync("Invalid Url or domain/subdomain isn't allowed homie");
                return;
            }

            // fetch the image
            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                await RespondAsync("Failed to download image");
                return;
            }

            var image = new Image(await response.Content.ReadAsStreamAsync());

            // upload the emote
            var createdEmote = await Context.Guild.CreateEmoteAsync(name, image);

            await RespondAsync($"Yoinked {createdEmote}");
        }
    }

}