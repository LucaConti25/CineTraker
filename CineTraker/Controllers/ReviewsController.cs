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
    public async Task<IActionResult> PostReview(Review review)
    {
        var movieExists = await _context.Movies.AnyAsync(m => m.Id == review.MovieId);
        if (!movieExists) return NotFound("La película no existe en tu base de datos.");

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
        if (id != updatedReview.Id) return BadRequest("El ID no coincide.");

        var review = await _context.Reviews.FindAsync(id);
        if (review == null) return NotFound("La reseña no existe.");

        review.Comment = updatedReview.Comment;
        review.Stars = updatedReview.Stars;
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return StatusCode(500, "Error al actualizar la base de datos.");
        }

        return NoContent();
    }

    
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteReview(int id)
    {
        var review = await _context.Reviews.FindAsync(id);
        if (review == null) return NotFound("Reseña no encontrada.");

        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync();

        return Ok(new { mensaje = $"Reseña con ID {id} eliminada correctamente." });
    }
}