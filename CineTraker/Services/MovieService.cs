using CineTraker.Data;
using CineTraker.Shared;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CineTraker.Services
{
    public class MovieService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey = "1860b29f";
        private readonly AppDbContext _context;

        public MovieService(AppDbContext context, HttpClient httpClient)
        {
            _httpClient = httpClient;
            _context = context;
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

        public async Task<Movie?> BuscarEnOmdbPorIdAsync(string imdbId)
        {
            
            string apiKey = "1860b29f";
            var response = await _httpClient.GetFromJsonAsync<OmdbResponse>($"https://www.omdbapi.com/?i={imdbId}&apikey={apiKey}");

            if (response != null && response.Response == "True")
            {
                return new Movie
                {
                    Title = response.Title,
                    Year = int.Parse(response.Year.Substring(0, 4)),
                    Director = response.Director,
                    Plot = response.Plot,
                    PosterUrl = response.Poster,
                    ImdbID = response.imdbID
                };
            }
            return null;
        }

        public async Task<int> EjecutarCargaMasiva(List<string> ids)
        {
            int contador = 0;
            var listaTemporal = new List<Movie>();

            foreach (var id in ids)
            {
                if (!await _context.Movies.AnyAsync(m => m.ImdbID == id))
                {
                    var peli = await BuscarEnOmdbPorIdAsync(id);
                    if (peli != null)
                    {
                        listaTemporal.Add(peli);
                        contador++;
                    }

                    await Task.Delay(250);
                }

                // Cada 10 películas, impactamos la base de datos para no llenar la RAM
                if (listaTemporal.Count >= 10)
                {
                    _context.Movies.AddRange(listaTemporal);
                    await _context.SaveChangesAsync();
                    listaTemporal.Clear();
                }
            }

            if (listaTemporal.Any())
            {
                _context.Movies.AddRange(listaTemporal);
                await _context.SaveChangesAsync();
            }
            return contador;
        }
    }
}