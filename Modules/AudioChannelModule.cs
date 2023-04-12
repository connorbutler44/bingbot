using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using System;

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
            if (channel == null) { await Context.Channel.SendMessageAsync("User must be in a voice channel, or a voice channel must be passed as an argument."); return; }

            await RespondAsync($"Joining channel ${Context.Channel.Name}");
            // For the next step with transmitting audio, you would want to pass this Audio Client in to a service.
            var audioClient = await channel.ConnectAsync();
            _audioChannelManager.Add(Context.Guild.Id, audioClient);
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