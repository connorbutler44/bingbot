using System;
using System.IO;
using System.Threading.Tasks;
using CliWrap;
using System.Threading;

public class YtDlpService
{
    public static async Task<TempMediaFile> DownloadStreamAsync(Uri url, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".mp4");

        var result = await Cli.Wrap("yt-dlp")
            .WithArguments(args => args
                // these two silent flags must be present otherwise yt-dlp throws an exception - possibly a code smell
                .Add("--quiet")
                .Add("--no-progress")
                .Add("-S")
                .Add("vcodec:h264,acodec:aac,ext:mp4")
                .Add("--merge-output-format")
                .Add("mp4")
                .Add("--remux-video")
                .Add("mp4")
                // move the moov atom to the front of the file so mobile clients can start playing
                // before the whole file has been fetched
                .Add("--postprocessor-args")
                .Add("ffmpeg:-movflags +faststart")
                .Add("-o")
                .Add(path)
                .Add(url.ToString()))
            .ExecuteAsync(cancellationToken);

        return new TempMediaFile(path);
    }
}