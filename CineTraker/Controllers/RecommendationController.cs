using CineTraker.Data;
using CineTraker.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
namespace CineTraker.Controllers
{
    [ApiController] 
    [Route("api/[controller]")]
    [Authorize]
    public class RecommendationController: ControllerBase
    {

        private readonly AppDbContext _context;

        public RecommendationController(AppDbContext context)
        {
            _context = context;
        }


        [HttpGet("grafo/{movieId}")]
        [AllowAnonymous]
        public async Task<ActionResult<MovieGraph>> GetGrafoRecomendacion(int movieId, [FromQuery] string excluidos = "")
        {
            var excludedIds = excluidos.Split(',', StringSplitOptions.RemoveEmptyEntries)
                               .Select(int.Parse).ToList();
            excludedIds.Add(movieId); 
            var movieBase = await _context.Movies.FindAsync(movieId);
            if (movieBase == null) return NotFound();

            var graph = new MovieGraph();

            // 1. Nodo Central
            graph.Nodos.Add(new MovieNode
            {
                Id = movieBase.Id,
                Titulo = movieBase.Title,
                PosterUrl = movieBase.PosterUrl,
                Grupo = "Central"
            });

            // --- RECOMENDACIÓN POR DIRECTOR ---
            var byDirector = await _context.Movies
            .Where(m => m.Director == movieBase.Director && !excludedIds.Contains(m.Id))
            .Take(1) // Bajamos a 3 para que sea más limpio
            .ToListAsync();

            foreach (var m in byDirector)
            {
                graph.Nodos.Add(new MovieNode 
                { 
                    Id = m.Id, 
                    Titulo = m.Title, 
                    PosterUrl = m.PosterUrl, 
                    Grupo = "Director" 
                });
                graph.Aristas.Add(new MovieEdge 
                { 
                    SourceId = movieBase.Id, 
                    TargetId = m.Id, 
                    Relacion = "Mismo Director" });
            }

            // --- RECOMENDACIÓN POR GÉNERO ---
            // Tomamos el primer género de la lista (ej: de "Action, Crime" sacamos "Action")
            var primerGenero = movieBase.Genre.Split(',')[0].Trim();

            var byGenre = await _context.Movies
            .Where(m => m.Genre.Contains(primerGenero) && !excludedIds.Contains(m.Id))
            .Where(m => !byDirector.Select(d => d.Id).Contains(m.Id))
            .Take(1)
            .ToListAsync();

            foreach (var m in byGenre)
            {
                graph.Nodos.Add(new MovieNode
                { 
                    Id = m.Id, 
                    Titulo = m.Title, 
                    PosterUrl = m.PosterUrl, 
                    Grupo = "Genre" 
                });
                graph.Aristas.Add(new MovieEdge
                { 
                    SourceId = movieBase.Id,
                    TargetId = m.Id,
                    Relacion = $"Género: {primerGenero}" 
                });
            }

            return Ok(graph);
        }
    }
}
