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
        private readonly MovieService _movieService;
        private readonly IConfiguration _configuration;

        // --- SISTEMA DE CACHÉ EN MEMORIA PARA VECTORES ---
        // Evita ahogar la base de datos con extracciones masivas al consultar similitudes
        private static Dictionary<int, (float[] vector, string genre, string rating)> _movieCache = new();
        private static DateTime _lastCacheSync = DateTime.MinValue;
        private static readonly SemaphoreSlim _cacheLock = new(1, 1);

        public RecommendationController(AppDbContext context, GeminiEmbeddingService geminiService, MovieService movieService, IConfiguration configuration)
        {
            _context = context;
            _geminiService = geminiService;
            _movieService = movieService;
            _configuration = configuration;
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

            // --- ACTUALIZAR CACHÉ (Si está vacío o viejo) ---
            if (DateTime.UtcNow - _lastCacheSync > TimeSpan.FromMinutes(30))
            {
                await _cacheLock.WaitAsync();
                try
                {
                    if (DateTime.UtcNow - _lastCacheSync > TimeSpan.FromMinutes(30))
                    {
                        var todasLasPeliculas = await _context.Movies
                            .Where(m => m.PlotEmbedding != null)
                            .Select(m => new { m.Id, m.PlotEmbedding, m.Genre, m.ImdbRating })
                            .ToListAsync();
                        
                        _movieCache.Clear();
                        foreach(var p in todasLasPeliculas)
                        {
                            if(p.PlotEmbedding != null)
                                _movieCache[p.Id] = (p.PlotEmbedding, p.Genre ?? "", p.ImdbRating ?? "0");
                        }
                        _lastCacheSync = DateTime.UtcNow;
                    }
                }
                finally
                {
                    _cacheLock.Release();
                }
            }

            int cantidadHijos = puntuacion >= 4.5 ? 3 : 2;
            List<CineTraker.Data.Entities.MovieEntity> recomendadas = new();

            // 5. Motor de Ranking Multidimensional
            var generosSemilla = movieBase.Genre?.Split(',').Select(g => g.Trim()).ToList() ?? new List<string>();
            var directorSemilla = movieBase.Director ?? "";
            var actoresSemilla = movieBase.Actors?.Split(',').Select(a => a.Trim()).ToList() ?? new List<string>();
            var añoSemilla = movieBase.Year;

            List<int> idsRecomendados = new();
            double mejorPuntajeLocal = 0;
            string modoRecomendacion = puntuacion >= 3.0 ? "Continuidad" : "Ruptura";

            // Cargar datos extra para cálculos multidimensionales (En un escenario ultra-escalable, todo esto iría en la caché)
            var extraDataDict = _context.Movies.Where(m => !excludedIds.Contains(m.Id))
                .Select(m => new { m.Id, m.Director, m.Actors, m.Year })
                .ToDictionary(x => x.Id, x => x);

            if (modoRecomendacion == "Continuidad") // CONTINUIDAD (ADN Similar con IA + Géneros + Casting + Época)
            {
                var candidatosLocal = _movieCache
                    .Where(k => !excludedIds.Contains(k.Key) && extraDataDict.ContainsKey(k.Key))
                    .Select(k => {
                        var similitudTrama = CalcularSimilitudCoseno(movieBase.PlotEmbedding, k.Value.vector);
                        
                        var generosCandidata = k.Value.genre.Split(',').Select(g => g.Trim()).ToList();
                        double afinidadGenero = generosSemilla.Count > 0 ? (double)generosSemilla.Intersect(generosCandidata).Count() / generosSemilla.Count : 0;
                        
                        var extraData = extraDataDict[k.Key];
                        double bonusDirector = (extraData.Director == directorSemilla && !string.IsNullOrEmpty(directorSemilla)) ? 1.0 : 0;
                        
                        var actoresCandidata = extraData.Actors?.Split(',').Select(a => a.Trim()).ToList() ?? new List<string>();
                        double afinidadCasting = actoresSemilla.Count > 0 ? (double)actoresSemilla.Intersect(actoresCandidata).Count() / actoresSemilla.Count : 0;
                        
                        int diferenciaAños = Math.Abs(añoSemilla - extraData.Year);
                        double afinidadTemporal = Math.Max(0, 1 - (diferenciaAños / 20.0)); // Cae a 0 si hay >20 años
                        
                        double.TryParse(k.Value.rating, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double rating);
                        double calidad = rating / 10.0; // 0 a 1

                        // Fórmula Multidimensional
                        double puntajeHibrido = (similitudTrama * 0.40) + (afinidadGenero * 0.25) + ((bonusDirector * 0.7 + afinidadCasting * 0.3) * 0.15) + (afinidadTemporal * 0.10) + (calidad * 0.10);
                        
                        return new { Id = k.Key, Score = puntajeHibrido };
                    })
                    .OrderByDescending(x => x.Score)
                    .ToList();

                idsRecomendados = candidatosLocal.Take(cantidadHijos).Select(x => x.Id).ToList();
                mejorPuntajeLocal = candidatosLocal.FirstOrDefault()?.Score ?? 0;
            }
            else // RUPTURA INTELIGENTE (Buscamos lo opuesto, con hilo conductor)
            {
                var candidatosLocal = _movieCache
                    .Where(k => !excludedIds.Contains(k.Key) && extraDataDict.ContainsKey(k.Key))
                    .Select(k => {
                        var similitudTrama = CalcularSimilitudCoseno(movieBase.PlotEmbedding, k.Value.vector);
                        var generosCandidata = k.Value.genre.Split(',').Select(g => g.Trim()).ToList();
                        int generosEnComun = generosSemilla.Intersect(generosCandidata).Count();
                        
                        // Penalizamos fuertemente si comparten géneros
                        double penalizacion = generosEnComun > 0 ? 0.5 : 0;
                        
                        var extraData = extraDataDict[k.Key];
                        double bonusDirector = (extraData.Director == directorSemilla && !string.IsNullOrEmpty(directorSemilla)) ? 0.2 : 0; // Hilo conductor
                        var actoresCandidata = extraData.Actors?.Split(',').Select(a => a.Trim()).ToList() ?? new List<string>();
                        double afinidadCasting = actoresSemilla.Count > 0 ? (double)actoresSemilla.Intersect(actoresCandidata).Count() / actoresSemilla.Count : 0;
                        
                        double.TryParse(k.Value.rating, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double rating);
                        double calidad = rating / 10.0;

                        // Queremos baja similitud, altísima calidad, e hilo conductor
                        double puntajeRuptura = (calidad * 0.40) + ((1 - similitudTrama) * 0.30) + ((bonusDirector + afinidadCasting) * 0.30) - penalizacion;
                        return new { Id = k.Key, Score = puntajeRuptura };
                    })
                    .OrderByDescending(x => x.Score)
                    .ToList();
                    
                idsRecomendados = candidatosLocal.Take(cantidadHijos).Select(x => x.Id).ToList();
                mejorPuntajeLocal = candidatosLocal.FirstOrDefault()?.Score ?? 0;
            }

            // --- EXPANSIÓN INFINITA (Tier 3) ---
            bool permitirExpansion = _configuration.GetValue<bool>("RecommendationSettings:AllowInfiniteExpansion", true);
            double umbralMinimo = _configuration.GetValue<double>("RecommendationSettings:MinimumLocalAffinityThreshold", 0.6);

            if (permitirExpansion && (idsRecomendados.Count < cantidadHijos || mejorPuntajeLocal < umbralMinimo))
            {
                var excluidosImdbIds = await _context.Movies.Where(m => excludedIds.Contains(m.Id)).Select(m => m.ImdbID).ToListAsync();
                
                // Pedimos ayuda a Gemini para salir de la burbuja
                var nuevosImdbIds = await _geminiService.GetRecommendationsAsync(movieBase.Title, directorSemilla, movieBase.Genre, modoRecomendacion, excluidosImdbIds!);

                // Iteramos para descargar y guardar los hallazgos en la base de datos
                foreach(var imdbId in nuevosImdbIds)
                {
                    if (string.IsNullOrEmpty(imdbId) || imdbId.Length < 3 || idsRecomendados.Count >= cantidadHijos) continue;

                    var peliExistente = await _context.Movies.FirstOrDefaultAsync(m => m.ImdbID == imdbId);
                    if (peliExistente != null)
                    {
                        if (!excludedIds.Contains(peliExistente.Id) && !idsRecomendados.Contains(peliExistente.Id)) 
                            idsRecomendados.Add(peliExistente.Id);
                        continue;
                    }

                    // Auto-Crecimiento: Bajar de OMDB, crear Embedding, y guardar
                    var omdbMovie = await _movieService.BuscarEnOmdbPorIdAsync(imdbId);
                    if (omdbMovie != null)
                    {
                        var nuevaEntidad = omdbMovie.ToEntity();
                        if(nuevaEntidad != null)
                        {
                            string textoAAnalizar = $"Géneros: {nuevaEntidad.Genre}. Sinopsis: {nuevaEntidad.Plot}";
                            nuevaEntidad.PlotEmbedding = await _geminiService.GetEmbeddingAsync(textoAAnalizar);
                            
                            _context.Movies.Add(nuevaEntidad);
                            await _context.SaveChangesAsync();

                            _lastCacheSync = DateTime.MinValue; // Forzar actualización de caché

                            if (!idsRecomendados.Contains(nuevaEntidad.Id)) idsRecomendados.Add(nuevaEntidad.Id);
                        }
                    }
                }
            }

            // Recuperamos las entidades completas
            recomendadas = await _context.Movies.Where(m => idsRecomendados.Contains(m.Id)).Take(cantidadHijos).ToListAsync();

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
