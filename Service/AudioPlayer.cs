using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CliWrap;
using Discord;
using Discord.Audio;

namespace Bingbot
{
    public class AudioPlayer : IAsyncDisposable
    {
        private readonly SemaphoreSlim _gate = new(1, 1);
        private AudioOutStream _pcmStream;

        public ulong ChannelId { get; }
        public IAudioClient AudioClient { get; }

        public bool IsConnected => AudioClient?.ConnectionState == ConnectionState.Connected;

        public AudioPlayer(ulong channelId, IAudioClient audioClient)
        {
            ChannelId = channelId;
            AudioClient = audioClient;
        }
        public async Task SendAudio(Stream inputStream)
        {
            if (!IsConnected)
                throw new InvalidOperationException($"Audio client not connected (state: {AudioClient?.ConnectionState.ToString() ?? "null"}).");

            using var pcm = await TranscodeAsync(inputStream);

            if (pcm.Length == 0)
                throw new InvalidOperationException("ffmpeg produced no PCM data.");

            // wait for previous audio segments to be played so it doesn't end up playing garbage
            await _gate.WaitAsync();
            try
            {
                // store PCM stream once and re-use it.
                // TODO: This is likely a code-smell, but it seems creating PCM streams for
                // each audio segment sent does not work with DiscordNET's voice API
                _pcmStream ??= AudioClient.CreatePCMStream(AudioApplication.Mixed);

                await pcm.CopyToAsync(_pcmStream);
                await _pcmStream.FlushAsync();
            }
            finally
            {
                _gate.Release();
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_pcmStream != null)
                await _pcmStream.DisposeAsync();

            AudioClient?.Dispose();
            _gate.Dispose();
        }

        private static async Task<MemoryStream> TranscodeAsync(Stream inputStream)
        {
            var outputStream = new MemoryStream();
            var stderr = new StringBuilder();

            var result = await Cli.Wrap("ffmpeg")
                .WithArguments(new[]
                {
                    "-hide_banner", "-nostats", "-loglevel", "error",
                    "-i", "pipe:0",
                    "-ac", "2", "-f", "s16le", "-ar", "48000",
                    "pipe:1"
                })
                .WithValidation(CommandResultValidation.None)
                .WithStandardInputPipe(PipeSource.FromStream(inputStream))
                .WithStandardOutputPipe(PipeTarget.ToStream(outputStream))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stderr))
                .ExecuteAsync();

            if (result.ExitCode != 0)
            {
                outputStream.Dispose();
                throw new InvalidOperationException($"ffmpeg exited with code {result.ExitCode}: {stderr}");
            }

            if (stderr.Length > 0)
                Console.WriteLine($"[ffmpeg] {stderr}");

            outputStream.Position = 0;
            return outputStream;
        }
    }
}