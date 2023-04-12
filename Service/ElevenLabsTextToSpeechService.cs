using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO;
using System.Net.Http.Json;

namespace Bingbot
{
    public class ElevenLabsTextToSpeechService : ITextToSpeechService
    {
        private readonly HttpClient client = new HttpClient();

        public Task<Stream> GetTextToSpeechAsync(string message, string voice, int? stability, int? clarity)
        {
            return GenerateMp3Segment(message, voice, stability, clarity);
        }

        private async Task<Stream> GenerateMp3Segment(string message, string speaker, int? stability, int? clarity)
        {
            string apiKey = Environment.GetEnvironmentVariable("ELEVEN_LABS_API_KEY");
            string url = $"https://api.elevenlabs.io/v1/text-to-speech/{speaker}";

            var request = new HttpRequestMessage(HttpMethod.Post, url);

            request.Content = generateRequestBody(message, stability, clarity);

            request.Headers.Add("xi-api-key", apiKey);
            request.Headers.Add("Accept", "audio/mpeg");

            HttpResponseMessage response = await client.SendAsync(request);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStreamAsync();
        }

        private JsonContent generateRequestBody(string message, int? stability, int? clarity)
        {
            // elevenlabs API errors if null values are passed for any of the voice_settings properties so we must
            // conditionally add those properties to the request. Probably a better way to do this but don't know c#
            // well enough yet 😿
            if (stability != null && clarity != null)
            {
                var content = new
                {
                    text = message,
                    voice_settings = new { stability = ((float)(long)stability) / 100, similarity_boost = ((float)(long)clarity) / 100 }
                };
                return JsonContent.Create(content);
            }
            else if (stability != null && clarity == null)
            {
                var content = new
                {
                    text = message,
                    voice_settings = new { stability = ((float)(long)stability) / 100, similarity_boost = 0.75 }
                };
                return JsonContent.Create(content);
            }
            else if (stability == null && clarity != null)
            {
                var content = new
                {
                    text = message,
                    voice_settings = new { stability = 0.75, similarity_boost = ((float)(long)clarity) / 100 }
                };
                return JsonContent.Create(content);
            }
            else
            {
                var content = new { text = message };
                return JsonContent.Create(content);
            }
        }
    }
}