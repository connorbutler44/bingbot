using System.Threading.Tasks;
using System.IO;

interface ITextToSpeechService
{
    Task<Stream> GetTextToSpeechAsync(string message, string voice, float? stability, float? clarity);
}