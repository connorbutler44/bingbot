using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.WebSocket;

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

        public async Task Add(ulong guildId, ulong voiceChannelId, IAudioClient audioClient)
        {
            var audioPlayer = new AudioPlayer(voiceChannelId, audioClient);

            audioClient.Disconnected += ex =>
            {
                Console.WriteLine($"[Audio] Disconnected from guild {guildId}: {ex?.Message}");
                _audioClients.TryRemove(guildId, out _);
                return Task.CompletedTask;
            };

            if (_audioClients.TryGetValue(guildId, out var previous) && !ReferenceEquals(previous.AudioClient, audioClient))
                await previous.DisposeAsync();

            _audioClients[guildId] = audioPlayer;
        }

        public AudioPlayer TryGet(SocketGuild guild)
        {
            var live = guild.AudioClient;

            if (live == null || live.ConnectionState != ConnectionState.Connected)
            {
                _ = RemoveAsync(guild.Id);
                return null;
            }

            if (_audioClients.TryGetValue(guild.Id, out var cached) && ReferenceEquals(cached.AudioClient, live))
                return cached;   // <- the hot path, hits on every send after the first

            var refreshed = new AudioPlayer(guild.Id, live);

            if (_audioClients.TryRemove(guild.Id, out var stale))
                _ = stale.DisposeAsync().AsTask();

            _audioClients[guild.Id] = refreshed;
            return refreshed;
        }

        public async Task RemoveAsync(ulong guildId)
        {
            if (_audioClients.TryRemove(guildId, out var audioPlayer))
                await audioPlayer.DisposeAsync();
        }
    }
}