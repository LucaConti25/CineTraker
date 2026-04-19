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

        [HttpGet("load/{seedMovieId}")]
        [Authorize]
        public async Task<ActionResult<UserMap>> LoadMap(int seedMovieId)
        {
            // Obtenemos el ID del usuario actual desde el token/cookie
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var map = await _context.UserMaps
                .FirstOrDefaultAsync(m => m.UserId == userId && m.SeedMovieId == seedMovieId);

            if (map == null) return NotFound(); 

            return Ok(map);
        }

        [HttpGet("my-maps")]
        [Authorize]
        public async Task<ActionResult<List<UserMap>>> GetUserMaps()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            return await _context.UserMaps
                .Where(m => m.UserId == userId)
                .OrderByDescending(m => m.CreatedDate)
                .ToListAsync();
        }
    }
}
