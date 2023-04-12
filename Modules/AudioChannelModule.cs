using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using System;
using Microsoft.Extensions.DependencyInjection;
using Discord.WebSocket;

namespace Bingbot.Modules
{
    public class AudioChannelModule : InteractionModuleBase<SocketInteractionContext>
    {
        IServiceProvider _provider;
        AudioChannelManager _audioChannelManager;
        ElevenLabsTextToSpeechService _textToSpeechService;

        public AudioChannelModule(IServiceProvider provider, AudioChannelManager audioChannelManager, ElevenLabsTextToSpeechService textToSpeechService)
        {
            _provider = provider;
            _audioChannelManager = audioChannelManager;
            _textToSpeechService = textToSpeechService;
        }

        [SlashCommand("join", "Join channel", runMode: RunMode.Async)]
        public async Task JoinChannel(IVoiceChannel channel = null)
        {
            // Get the audio channel
            channel = channel ?? (Context.User as IGuildUser)?.VoiceChannel;
            if (channel == null) { await RespondAsync("You must be in a voice channel, or a voice channel must be passed as an argument", ephemeral: true); return; }

            await RespondAsync($"Joining channel {Context.Channel.Name}", ephemeral: true);

            var audioClient = await channel.ConnectAsync();
            _audioChannelManager.Add(Context.Guild.Id, Context.Channel.Id, audioClient);
        }

        [SlashCommand("disconnect", "Disconnect from channel", runMode: RunMode.Async)]
        public async Task DisconnectChannel()
        {
            var discordClient = _provider.GetRequiredService<DiscordSocketClient>();

            try
            {
                var botUser = Context.Guild.GetUser(discordClient.CurrentUser.Id);
                await botUser.VoiceChannel.DisconnectAsync();
                _audioChannelManager.TryRemove(Context.Guild.Id);
                await RespondAsync("Disconnected from voice channel", ephemeral: true);
            }
            catch (Exception)
            {
                await RespondAsync("No voice channel to disconnect from", ephemeral: true);
            }
        }

        [SlashCommand("send-audio", "Send audio", runMode: RunMode.Async)]
        public async Task SendAudio(string text)
        {
            var client = _audioChannelManager.Get(Context.Guild.Id);
            await RespondAsync("Submitting audio", ephemeral: true);
            await client.SendAudio(text);
        }
    }
}