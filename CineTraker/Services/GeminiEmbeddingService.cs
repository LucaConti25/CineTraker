using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace CineTraker.Services
{
    public class GeminiEmbeddingService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public GeminiEmbeddingService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _apiKey = config["Gemini:ApiKey"] ?? throw new ArgumentNullException("Falta la API Key de Gemini");
        }

        public async Task<float[]> GetEmbeddingAsync(string text)
        {
            // 1. Cambiamos la URL al modelo nuevo
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-embedding-001:embedContent?key={_apiKey}";

            var requestBody = new
            {
                // 2. Actualizamos el nombre en el JSON
                model = "models/gemini-embedding-001",
                content = new
                {
                    parts = new[] { new { text = text } }
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, jsonContent);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var jsonDocument = JsonDocument.Parse(responseString);

            var values = jsonDocument.RootElement
                .GetProperty("embedding")
                .GetProperty("values")
                .EnumerateArray()
                .Select(x => x.GetSingle())
                .ToArray();

            return values;
        }
    }
}
