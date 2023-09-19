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

        [SlashCommand("send-audio", "Send audio to voice channel", runMode: RunMode.Async)]
        public async Task SendAudio(
            [Summary(description: "Voice to be used for tts")]
            [Choice("Obama", "b5EjCnCMCw9XA8W0FFMT"), Choice("Jarrad", "iKfvwsmhoDNziMjUHyUo"),
                Choice("Asher", "QBaMLEOzUYxyKKh7ZuZN"), Choice("Todd Howard", "8oLT3oOTUp9RV6GHl7AU"),
                Choice("Whopper", "Zuzo46BJSET6252mCZX5"), Choice("Tim Gunn", "lQV6YBaetZO5fb2n1JSV"),
                Choice("Halo Announcer", "2mY0k5zCDvLApJhuUvS4"), Choice("Cortana", "G5JvFs8Ivxbn4PsafONN"),
                Choice("Chills", "XmxA2cgmSD8ydzjAjP7G"), Choice("Oblivion Guard", "S4MX3ES8njsedM3zBZhJ"),
                Choice("Adoring Fan", "uXUHVbOprp9jBZRr8msZ"), Choice("Girly", "EHEP5eqnatUfacCbMlOR"),
                Choice("Richard Mealey", "KRHlgM8XFdAaiKgbyzx9"),
                Choice("Deep Voice British Mans", "4OBaNu67U8XfuTFNHrsm")] string voice,
            [Summary(description: "Text to be used for tts")]
            string text,
            [Summary(description: "0-100. Higher values: consistency + monotonality. Lower values: More expressive + instability.")]
            int? stability = null,
            [Summary(description: "0-100. Higher values for better clarity but could cause artifacting. Lower if artifacts is present.")]
            int? clarity = null)
        {
            var client = _audioChannelManager.TryGet(Context.Guild.Id);

            if (client == null)
            {
                await RespondAsync("Bingbot must be in a voice channel to send audio", ephemeral: true);
                return;
            }

            await RespondAsync("Submitting audio", ephemeral: true);
            var inputStream = await _textToSpeechService.GetTextToSpeechAsync(text, voice, stability, clarity);
            await client.SendAudio(inputStream);
        }
    }
}