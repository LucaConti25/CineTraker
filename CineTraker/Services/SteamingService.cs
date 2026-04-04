using CineTraker.Shared;
using System.Net.Http.Json;

namespace CineTraker.Services
{
    public class StreamingService
    {
        private readonly HttpClient _http;
        private readonly string _apiKey = "YvqovyOXal4AE9I1jCchCdUNWzk6yn3wr57fAwI5"; 

        public StreamingService(HttpClient http) => _http = http;

        public async Task<List<StreamingSource>> GetSourcesAsync(string imdbId)
        {
            try
            {
                
                // de aca obtengo el ID interno
                var detailsUrl = $"https://api.watchmode.com/v1/title/{imdbId}/details/?apiKey={_apiKey}";
                var details = await _http.GetFromJsonAsync<WatchmodeDetails>(detailsUrl);

                if (details == null) return new List<StreamingSource>();


                var sourcesUrl = $"https://api.watchmode.com/v1/title/{details.Id}/sources/?apiKey={_apiKey}&regions=AR";
                var response = await _http.GetFromJsonAsync<List<WatchmodeSource>>(sourcesUrl);

                
                return response?.Select(s => new StreamingSource
                {
                    Name = s.Name,
                    Type = s.Type, 
                    WebUrl = s.WebUrl,
                    LogoUrl = GetLogoUrl(s.Name)
                }).DistinctBy(x => x.Name).ToList() ?? new();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en Watchmode: {ex.Message}");
                return new List<StreamingSource>();
            }
        }

        private string GetLogoUrl(string providerName)
        {
            var name = providerName.ToLower();

            return name switch
            {
                var n when n.Contains("netflix") => "/images/logos/netflix.png",
                var n when n.Contains("max") || n.Contains("hbo") => "/images/logos/hbo.jpg",
                var n when n.Contains("disney") => "/images/logos/disney+.png",
                var n when n.Contains("amazon") || n.Contains("prime") => "/images/logos/prime.jpg",
                var n when n.Contains("paramount") => "/images/logos/paramount.jpg",
                var n when n.Contains("apple") => "/images/logos/apple-tv.png",
                var n when n.Contains("mubi") => "/images/logos/mubi.png",
                var n when n.Contains("claro") => "/images/logos/claro.jpg",
                _ => "" // Fallback vacío
            };
        }


    }

    public class WatchmodeDetails
    {
        public int Id { get; set; } // El ID que Watchmode usa internamente
    }

    public class WatchmodeSource
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string WebUrl { get; set; } = "";
    }

    
}
