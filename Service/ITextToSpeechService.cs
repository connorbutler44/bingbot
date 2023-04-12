using System.Threading.Tasks;
using System.IO;

public interface ITextToSpeechService
{
    Task<Stream> GetTextToSpeechAsync(string message, string voice, int? stability, int? clarity);
}