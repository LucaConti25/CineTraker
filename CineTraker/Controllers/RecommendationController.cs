using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CineTraker.Shared;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using CineTraker.Data;
using CineTraker.Shared.Models;
using CineTraker.Services;

namespace CineTraker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RecommendationController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly GeminiEmbeddingService _geminiService;

        public RecommendationController(AppDbContext context, GeminiEmbeddingService geminiService)
        {
            _context = context;
            _geminiService = geminiService;
        }

        [HttpGet("grafo/{movieId}")]
        public async Task<ActionResult<MovieGraph>> GetGrafoRecomendacion(int movieId, [FromQuery] string excluidos = "")
        {
            // 1. Usuario y Película Base
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var movieBase = await _context.Movies.FindAsync(movieId);
            if (movieBase == null) return NotFound();

            // 2. Determinar Puntuación
            var ultimaReview = await _context.Reviews
        .Where(r => r.MovieId == movieId && r.UserId == userId)
        .OrderByDescending(r => r.CreatedAt)
        .FirstOrDefaultAsync();

            double puntuacion = ultimaReview?.Stars ?? 3.0;

            // 3. Preparar Grafo y Lista de Exclusión
            var graph = new MovieGraph();
            var excludedIds = excluidos.Split(',', StringSplitOptions.RemoveEmptyEntries)
                         .Select(int.Parse).ToList();

            var vistasIds = await _context.Reviews
              .Where(r => r.UserId == userId)
              .Select(r => r.MovieId)
              .ToListAsync();

            excludedIds.AddRange(vistasIds);
            if (!excludedIds.Contains(movieId)) excludedIds.Add(movieId);

            // Nodo Central
            graph.Nodos.Add(new MovieNode
            {
                Id = movieBase.Id,
                Titulo = movieBase.Title,
                PosterUrl = movieBase.PosterUrl,
                Grupo = "Central",
                Score = puntuacion
            });

            // 4. Lógica de Ramificación
            bool esSemilla = excludedIds.Count <= 1;
            if (puntuacion <= 1.5 && !esSemilla) return Ok(graph); // PODA

            // Si es semilla con mala nota, forzamos ruptura
            if (puntuacion <= 1.5 && esSemilla) puntuacion = 2.0;

            int cantidadHijos = puntuacion >= 4.5 ? 3 : 2;
            List<Movie> recomendadas = new();

            // 5. Motor de Ranking (Heurística)
            var generosSemilla = movieBase.Genre?.Split(',').Select(g => g.Trim()).ToList() ?? new List<string>();

            if (puntuacion >= 3.0) // CONTINUIDAD (ADN Similar con IA)
            {
                // 1. Traemos las películas candidatas que tengan vector generado
                var poolCandidatas = await _context.Movies
          .Where(m => !excludedIds.Contains(m.Id) && m.PlotEmbedding != null)
          .ToListAsync();

                // 2. Las ordenamos usando la IA (Similitud Coseno)
                recomendadas = poolCandidatas.Select(m => new {
                    Peli = m,
                    // Comparamos el vector de la película original contra cada candidata
                    Afinidad = CalcularSimilitudCoseno(movieBase.PlotEmbedding, m.PlotEmbedding)
                })
        .OrderByDescending(x => x.Afinidad) // La más cercana a 1.0 es la más parecida
                .Take(cantidadHijos)
        .Select(x => x.Peli)
        .ToList();
            }
            else // RUPTURA (2 estrellas)
            {
                // Buscamos pelis que NO compartan el género principal
                var primerGenero = generosSemilla.FirstOrDefault() ?? "";
                recomendadas = await _context.Movies
                  .Where(m => !excludedIds.Contains(m.Id) && !m.Genre.Contains(primerGenero))
                  .OrderBy(x => Guid.NewGuid()) // En ruptura el azar es bueno
                            .OrderByDescending(m => m.ImdbRating) // Pero que sean buenas
                            .Take(cantidadHijos)
                  .ToListAsync();
            }

            // 6. Construir el Grafo
            foreach (var m in recomendadas)
            {
                graph.Nodos.Add(new MovieNode
                {
                    Id = m.Id,
                    Titulo = m.Title,
                    PosterUrl = m.PosterUrl,
                    Grupo = puntuacion >= 3.0 ? "Continuidad" : "Ruptura"
                });

                graph.Aristas.Add(new MovieEdge
                {
                    SourceId = movieBase.Id,
                    TargetId = m.Id,
                    Relacion = puntuacion >= 3.0 ? "ADN Similar" : "Cambio de aires"
                });
            }

            return Ok(graph);
        }

        [HttpPost("generar-embeddings")]
        [AllowAnonymous] // Para poder ejecutarlo fácil desde Swagger sin el Token
        public async Task<IActionResult> GenerarEmbeddings()
        {
            var peliculasSinVector = await _context.Movies
        .Where(m => m.PlotEmbedding == null && !string.IsNullOrEmpty(m.Plot))
        .ToListAsync();

            if (!peliculasSinVector.Any())
                return Ok("Todas las películas ya tienen sus embeddings generados.");

            int procesadas = 0;

            foreach (var movie in peliculasSinVector)
            {
                try
                {
                    string textoAAnalizar = $"Géneros: {movie.Genre}. Sinopsis: {movie.Plot}";

                    float[] vector = await _geminiService.GetEmbeddingAsync(textoAAnalizar);

                    movie.PlotEmbedding = vector;
                    procesadas++;

                    await Task.Delay(1500);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error procesando ID {movie.Id} - {movie.Title}: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();

            return Ok($"Proceso finalizado con éxito. Se generaron {procesadas} nuevos vectores.");
        }

        private double CalcularSimilitudCoseno(float[] vectorA, float[] vectorB)
        {
            if (vectorA == null || vectorB == null || vectorA.Length != vectorB.Length)
                return 0;

            double dotProduct = 0;
            double magnitudeA = 0;
            double magnitudeB = 0;

            for (int i = 0; i < vectorA.Length; i++)
            {
                dotProduct += vectorA[i] * vectorB[i];
                magnitudeA += Math.Pow(vectorA[i], 2);
                magnitudeB += Math.Pow(vectorB[i], 2);
            }

            if (magnitudeA == 0 || magnitudeB == 0) return 0;

            return dotProduct / (Math.Sqrt(magnitudeA) * Math.Sqrt(magnitudeB));
        }
    }
}
