using System.Threading.Tasks;
using Discord.Audio;
using CliWrap;
using System.IO;
using System;

namespace Bingbot
{
    public class AudioPlayer
    {
        public ulong _channelId;
        IAudioClient _audioClient;
        AudioOutStream _outputStream;

        public AudioPlayer(ulong channelId, IAudioClient audioClient, AudioOutStream outputStream)
        {
            _channelId = channelId;
            _audioClient = audioClient;
            _outputStream = outputStream;
        }

        public async Task SendAudio(Stream inputStream)
        {
            try
            {
                var stream = await WriteToStream(inputStream);
                await stream.CopyToAsync(_outputStream);
            }
            finally
            {
                await _outputStream.FlushAsync();
            }
        }

        private async Task<Stream> WriteToStream(Stream inputStream)
        {
            var outputStream = new MemoryStream();

            var result = await Cli.Wrap("ffmpeg")
                .WithArguments(new[] { "-hide_banner", "-nostats", "-loglevel", "error", "-i", "pipe:", "-ac", "2", "-f", "s16le", "-ar", "48000", "pipe:1" })
                .WithValidation(CommandResultValidation.None)
                .WithStandardInputPipe(PipeSource.FromStream(inputStream))
                .WithStandardOutputPipe(PipeTarget.ToStream(outputStream))
                .ExecuteAsync();

            outputStream.Position = 0;

            Console.WriteLine(result.ExitCode);
            return outputStream;
        }
    }
}