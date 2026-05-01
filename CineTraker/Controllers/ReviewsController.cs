using CineTraker.Data;
using CineTraker.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class ReviewsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ReviewsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> PostReview(Review review)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        review.UserId = userId;
        review.CreatedAt = DateTime.Now;

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();
        return Ok(review);
    }


    [AllowAnonymous]
    [HttpGet("movie/{movieId}")]
    public async Task<ActionResult<IEnumerable<Review>>> GetReviewsByMovie(int movieId)
    {
        // Buscamos las reviews filtradas por película
        var reviews = await _context.Reviews
            .Where(r => r.MovieId == movieId)
            .OrderByDescending(r => r.Id) // Las más recientes arriba
            .ToListAsync();

        // Siempre devolvemos una lista, cumpliendo el contrato con el Frontend
        return Ok(reviews);
    }


    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateReview(int id, Review updatedReview)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (id != updatedReview.Id) return BadRequest("El ID no coincide.");

        var review = await _context.Reviews.FindAsync(id);
        if (review == null) return NotFound("La reseña no existe.");

        if (review.UserId != userId)
        {
            return Forbid("No tenés permiso para editar esta reseña.");
        }

        review.Comment = updatedReview.Comment;
        review.Stars = updatedReview.Stars;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return StatusCode(500, "Error de concurrencia al actualizar.");
        }

        return NoContent();
    }


    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteReview(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var review = await _context.Reviews.FindAsync(id);

        if (review == null) return NotFound();

        // Validamos que la reseña pertenezca a quien pide borrarla
        if (review.UserId != userId) return Forbid();

        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync();

        return Ok();
    }


    [HttpGet("user")]
    public async Task<ActionResult<IEnumerable<Review>>> GetUserReviews()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        // Ahora EF usará correctamente la relación que definiste
        return await _context.Reviews
            .Include(r => r.Movie)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.Id)
            .ToListAsync();
    }
}