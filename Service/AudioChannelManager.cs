using System;
using System.Collections.Concurrent;
using Discord.Audio;

namespace Bingbot
{
    public class AudioChannelManager
    {
        IServiceProvider _serviceProvider;

        ConcurrentDictionary<ulong, AudioPlayer> _audioClients = new();

        public AudioChannelManager(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void Add(ulong guildID, ulong channelId, IAudioClient audioClient)
        {
            var outputStream = audioClient.CreatePCMStream(AudioApplication.Mixed);

            var audioPlayer = new AudioPlayer(channelId, audioClient, outputStream);
            _audioClients[guildID] = audioPlayer;
        }

        public AudioPlayer TryRemove(ulong guildID)
        {
            var removed = _audioClients.TryRemove(guildID, out AudioPlayer audioPlayer);

            return audioPlayer;
        }

        public AudioPlayer TryGet(ulong guildID)
        {
            AudioPlayer audioPlayer;

            _audioClients.TryGetValue(guildID, out audioPlayer);

            return audioPlayer;
        }
    }
}