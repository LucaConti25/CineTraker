using CineTraker.Data;
using CineTraker.Shared;
using CineTraker.Shared.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CineTraker.Services
{
    public class MovieService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey = "35405d4a";
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
            
            
            var response = await _httpClient.GetFromJsonAsync<OmdbResponse>($"https://www.omdbapi.com/?i={imdbId}&apikey={_apiKey}");

            if (response != null && response.Response == "True")
            {

                var runtimeLimpio = response.Runtime?.Replace(" min", "").Replace("N/A", "0");
                

                return new Movie
                {
                    Title = response.Title,
                    Year = int.TryParse(response.Year.Substring(0, 4), out int y) ? y : 0,
                    Director = response.Director,
                    Plot = response.Plot,
                    PosterUrl = response.Poster,
                    ImdbID = response.imdbID,
                    Genre = response.Genre,
                    Runtime = int.TryParse(runtimeLimpio, out int r) ? r : 0,
                    Actors = response.Actors,
                    Rated = response.Rated,
                    ImdbRating = response.imdbRating
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





        public async Task<List<Movie>> SearchMoviesFromApiAsync(string title)
        {
            
            var response = await _httpClient.GetFromJsonAsync<OmdbSearchResponse>(
                $"https://www.omdbapi.com/?s={title}&apikey={_apiKey}");

            if (response == null || response.Response == "False" || response.Search == null)
            {
                return new List<Movie>();
            }

            // Convertimos la respuesta cruda de OMDb a nuestro modelo de dominio 'Movie'.
            return response.Search.Select(m => new Movie
            {
                Title = m.Title,
                Year = int.TryParse(m.Year.Substring(0, 4), out int year) ? year : 0,
                PosterUrl = m.Poster,
                ImdbID = m.imdbID
            }).ToList();
        }


    }
}