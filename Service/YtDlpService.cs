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
                .Add("-o")
                .Add(path)
                .Add(url.ToString()))
            .ExecuteAsync(cancellationToken);

        return new TempMediaFile(path);
    }
}