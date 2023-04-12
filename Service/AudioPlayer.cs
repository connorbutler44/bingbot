using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Discord.Audio;

namespace Bingbot
{
    public class AudioPlayer
    {
        IAudioClient _audioClient;
        AudioOutStream _outputStream;

        public AudioPlayer(IAudioClient audioClient, AudioOutStream outputStream)
        {
            _audioClient = audioClient;
            _outputStream = outputStream;
        }

        public async Task SendAudio(string path)
        {
            using (var ffmpeg = CreateStream(Path.GetFullPath(Path.Combine(".", path))))
            using (var inputStream = ffmpeg.StandardOutput.BaseStream)
            // using (var outputStream = _audioClient.CreatePCMStream(AudioApplication.Mixed))
            {
                try
                {
                    await inputStream.CopyToAsync(_outputStream);
                }
                finally
                {
                    await _outputStream.FlushAsync();
                }
            }
        }

        private Process CreateStream(string path)
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-hide_banner -loglevel panic -i {path} -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            });
        }
    }
}