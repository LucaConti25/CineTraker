using Microsoft.AspNetCore.Mvc;
using CineTraker.Data;
using CineTraker.Models;
using Microsoft.EntityFrameworkCore;

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



    [HttpGet("movie/{movieId}")]
    public async Task<IActionResult> GetReviewsByMovie(int movieId)
    {
        var movieExists = await _context.Movies.AnyAsync(m => m.Id == movieId);

        if (!movieExists)
        {
            return NotFound(new
            {
                error = "Película no encontrada",
                detalle = $"No existe ninguna película con el ID {movieId} en CineTraker."
            });
        }

        var reviews = await _context.Reviews
            .Where(r => r.MovieId == movieId)
            .ToListAsync();

        if (reviews == null || !reviews.Any())
        {
            return Ok(new
            {
                mensaje = "Esta película aún no tiene reseñas.",
                sugerencia = "¡Podés ser el primero en calificarla usando el método POST!"
            });
        }

        return Ok(reviews);
    }
}