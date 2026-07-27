using CineTraker.Data;
using CineTraker.Data.Entities;
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

        var entity = review.ToEntity();
        if (entity == null) return BadRequest();

        entity.UserId = userId;
        entity.CreatedAt = DateTime.Now;

        _context.Reviews.Add(entity);
        await _context.SaveChangesAsync();
        return Ok(entity.ToDto());
    }


    [AllowAnonymous]
    [HttpGet("movie/{movieId}")]
    public async Task<ActionResult<IEnumerable<Review>>> GetReviewsByMovie(int movieId)
    {
        var reviews = await _context.Reviews
            .Where(r => r.MovieId == movieId)
            .OrderByDescending(r => r.Id)
            .ToListAsync();

        return Ok(reviews.Select(r => r.ToDto()).ToList());
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

        if (review.UserId != userId) return Forbid();

        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync();

        return Ok();
    }


    [HttpGet("user")]
    public async Task<ActionResult<IEnumerable<Review>>> GetUserReviews()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var reviews = await _context.Reviews
            .Include(r => r.Movie)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.Id)
            .ToListAsync();
            
        return reviews.Select(r => r.ToDto()).Where(r => r != null).Cast<Review>().ToList();
    }

    [Authorize]
    [HttpGet("my-recent-reviews")]
    public async Task<ActionResult<IEnumerable<Review>>> GetMyRecentReviews()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var recentReviews = await _context.Reviews 
            .Include(r => r.Movie) 
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(5)
            .ToListAsync();

        return Ok(recentReviews.Select(r => r.ToDto()).ToList());
    }
}