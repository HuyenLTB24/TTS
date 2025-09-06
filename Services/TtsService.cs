using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TTS.Services
{
    public class TtsService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "http://localhost:5050/v1/audio/speech";

        public TtsService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromMinutes(5); // Allow time for longer texts
        }

        public async Task<bool> IsServerRunningAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(BaseUrl.Replace("/speech", "/health"));
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<byte[]> GenerateSpeechAsync(string text, string voice)
        {
            var requestBody = new
            {
                model = "gpt-4o-mini-tts",
                voice = voice,
                input = text
            };

            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(BaseUrl, content);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsByteArrayAsync();
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}