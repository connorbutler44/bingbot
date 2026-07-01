using System;
using System.IO;
using System.Threading.Tasks;

public sealed class TempMediaFile : IAsyncDisposable
{
    public string Path { get; }

    public TempMediaFile(string path)
    {
        Path = path;
    }

    public async ValueTask DisposeAsync()
    {
        if (File.Exists(Path))
        {
            File.Delete(Path);
        }

        await ValueTask.CompletedTask;
    }
}