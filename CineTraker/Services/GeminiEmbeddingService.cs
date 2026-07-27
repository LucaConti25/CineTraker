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

        public async Task<List<string>> GetRecommendationsAsync(string title, string director, string genre, string mode, List<string> excludedImdbIds)
        {
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={_apiKey}";
            
            string modeInstructions = mode == "Continuidad" 
                ? "Recomienda películas que sean similares en trama, género y que idealmente compartan el mismo director, estilo visual o época."
                : "Recomienda películas de géneros completamente diferentes (Ruptura), pero intenta mantener un hilo conductor (como el mismo director, o actor principal) y asegúrate de que tengan excelente calificación (Imdb Rating > 7.5).";

            string prompt = $@"
Eres el motor de recomendación de CineTraker. 
La película base es '{title}' (Director: {director}, Géneros: {genre}).
{modeInstructions}
No incluyas estas películas (IDs de IMDB): {string.Join(", ", excludedImdbIds)}.

Devuelve EXCLUSIVAMENTE un arreglo JSON con 3 IDs de IMDB (ej: tt1234567) de las recomendaciones. NADA MÁS. Sin formato markdown.
Ejemplo de salida exacta:
[""tt0111161"", ""tt0468569"", ""tt0137523""]";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[] { new { text = prompt } }
                    }
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, jsonContent);
            
            if (!response.IsSuccessStatusCode) return new List<string>();

            var responseString = await response.Content.ReadAsStringAsync();
            using var jsonDocument = JsonDocument.Parse(responseString);
            
            try 
            {
                var textResult = jsonDocument.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text").GetString();

                textResult = textResult?.Replace("```json", "").Replace("```", "").Trim() ?? "[]";
                var result = JsonSerializer.Deserialize<List<string>>(textResult);
                return result ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }
    }
}
