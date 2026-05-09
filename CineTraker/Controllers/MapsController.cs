using CineTraker.Data;
using CineTraker.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CineTraker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] 
    public class MapsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public MapsController(AppDbContext context) => _context = context;

        [HttpPost("save")]
        public async Task<IActionResult> SaveMap([FromBody] UserMap map)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            map.UserId = userId;

            if (map.Id == 0) _context.UserMaps.Add(map);
            else _context.Entry(map).State = EntityState.Modified;

            await _context.SaveChangesAsync();
            return Ok(map);
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<UserMap>> GetMap(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var map = await _context.UserMaps
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);

            if (map == null) return NotFound();

            return Ok(map);
        }

        
        [HttpGet("my-maps")]
        [Authorize]
        public async Task<ActionResult<List<UserMap>>> GetUserMaps()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            return await _context.UserMaps
                .Include(m => m.SeedMovie) // <--- CLAVE para tener el póster y título
                .Where(m => m.UserId == userId)
                .OrderByDescending(m => m.CreatedDate)
                .ToListAsync();
        }

        
        [HttpPost("create/{movieId}")]
        [Authorize]
        public async Task<ActionResult<UserMap>> CreateMap(int movieId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            
            var hasReview = await _context.Reviews
                .AnyAsync(r => r.UserId == userId && r.MovieId == movieId);

            if (!hasReview)
                return BadRequest("Debes reseñar la película primero para iniciar un mapa.");

            var movie = await _context.Movies.FindAsync(movieId);

            var newMap = new UserMap
            {
                UserId = userId,
                SeedMovieId = movieId,
                Name = movie != null ? $"Expedición: {movie.Title}" : "Nueva Expedición",
                GraphJson = "", 
                CreatedDate = DateTime.Now,
                TotalMovies = 1,
                WatchedMovies = 1
            };

            _context.UserMaps.Add(newMap);
            await _context.SaveChangesAsync();

            return Ok(newMap);
        }
    }
}

