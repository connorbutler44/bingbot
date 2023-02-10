using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO;
using System.Net.Http.Json;

namespace Bingbot
{
    class ElevenLabsTextToSpeechService : ITextToSpeechService
    {
        private readonly HttpClient client = new HttpClient();

        public Task<Stream> GetTextToSpeechAsync(string message, string voice)
        {
            return GenerateMp3Segment(message, voice);
        }

        private async Task<Stream> GenerateMp3Segment(string message, string speaker)
        {
            string apiKey = Environment.GetEnvironmentVariable("ELEVEN_LABS_API_KEY");
            string url = $"https://api.elevenlabs.io/v1/text-to-speech/{speaker}";

            var request = new HttpRequestMessage(HttpMethod.Post, url);

            request.Content = JsonContent.Create(new { text = message });

            request.Headers.Add("xi-api-key", apiKey);
            request.Headers.Add("Accept", "audio/mpeg");

            HttpResponseMessage response = await client.SendAsync(request);

            response.EnsureSuccessStatusCode();

            var test = await response.Content.ReadAsStringAsync();

            return await response.Content.ReadAsStreamAsync();
        }
    }
}