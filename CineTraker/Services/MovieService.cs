using CineTraker.Models;
using System.Text.Json;

namespace CineTraker.Services
{
    public class MovieService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey = "1860b29f"; 

        public MovieService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<OmdbResponse?> GetMovieFromApiAsync(string title)
        {
            
            var response = await _httpClient.GetAsync($"https://www.omdbapi.com/?t={title}&apikey={_apiKey}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<OmdbResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            return null;
        }
    }
}