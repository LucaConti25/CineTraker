using CineTraker.Data;
using CineTraker.Data.Entities;
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
            var entity = map.ToEntity();
            if (entity == null) return BadRequest();
            
            entity.UserId = userId;

            if (map.Id == 0) _context.UserMaps.Add(entity);
            else _context.Entry(entity).State = EntityState.Modified;

            await _context.SaveChangesAsync();
            return Ok(entity.ToDto());
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<UserMap>> GetMap(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var map = await _context.UserMaps
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);

            if (map == null) return NotFound();

            return Ok(map.ToDto());
        }

        
        [HttpGet("my-maps")]
        [Authorize]
        public async Task<ActionResult<List<UserMap>>> GetUserMaps()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var maps = await _context.UserMaps
                .Include(m => m.SeedMovie) 
                .Where(m => m.UserId == userId)
                .OrderByDescending(m => m.CreatedDate)
                .ToListAsync();
                
            return maps.Select(m => m.ToDto()).Where(m => m != null).Cast<UserMap>().ToList();
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

            var newMap = new UserMapEntity
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

            return Ok(newMap.ToDto());
        }

        [Authorize]
        [HttpGet("my-recent-maps")]
        public async Task<ActionResult<IEnumerable<UserMap>>> GetMyRecentMaps() 
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var recentMaps = await _context.UserMaps 
                .Where(m => m.UserId == userId)
                .OrderByDescending(m => m.CreatedDate)
                .Take(5)
                .ToListAsync();

            return Ok(recentMaps.Select(m => m.ToDto()).ToList());
        }
    }
}
