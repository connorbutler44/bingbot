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

        public void Add(ulong guildID, IAudioClient audioClient)
        {
            var outputStream = audioClient.CreatePCMStream(AudioApplication.Mixed);

            var audioPlayer = new AudioPlayer(audioClient, outputStream);
            _audioClients[guildID] = audioPlayer;
        }

        public AudioPlayer Get(ulong guildID)
        {
            return _audioClients[guildID];
        }
    }
}