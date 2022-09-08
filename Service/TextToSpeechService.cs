using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Bingbot
{
    class TextToSpeechService
    {
        private readonly HttpClient client = new HttpClient();

        public async Task<Stream> GetTextToSpeechAsync(string message, string voice)
        {
            var output = new List<byte>();

            foreach (string chunk in message.SplitOnWords(200))
            {
                var segment = await GenerateMp3Segment(chunk, voice);
                output.AddRange(segment);
            }

            return new MemoryStream(output.ToArray());
        }

        private async Task<byte[]> GenerateMp3Segment(string message, string speaker)
        {
            if (message.Length > 200)
                throw new ArgumentOutOfRangeException("Message must be 200 characters or less");

            string cookie = Environment.GetEnvironmentVariable("TTS_COOKIE");
            string host = Environment.GetEnvironmentVariable("TTS_HOST");

            string url = $"https://{host}/media/api/text/speech/invoke/?text_speaker={speaker}&req_text={message}&speaker_map_type=0&aid=1233";
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            // must provide sessionid to tiktok otherwise request is rejected (this may be fragile and need to be updated over time)
            request.Headers.Add("Cookie", cookie);
            HttpResponseMessage response = await client.SendAsync(request);

            response.EnsureSuccessStatusCode();

            var contentStream = await response.Content.ReadAsStreamAsync();
            using var streamReader = new StreamReader(contentStream);
            using var jsonReader = new JsonTextReader(streamReader);

            JsonSerializer serializer = new JsonSerializer();

            TtsResponse ttsData = serializer.Deserialize<TtsResponse>(jsonReader);

            byte[] data = Convert.FromBase64String(ttsData.data.v_str);

            return data;
        }
    }
}