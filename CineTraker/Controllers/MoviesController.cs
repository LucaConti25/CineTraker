
using CineTraker.Data;
using CineTraker.Models;
using CineTraker.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/[controller]")]
[ApiController]
public class MoviesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly MovieService _movieService;

    public MoviesController(AppDbContext context, MovieService movieService)
    {
        _context = context;
        _movieService = movieService;
    }


    [HttpGet]
    public async Task<ActionResult<IEnumerable<Movie>>> GetMovies()
    {
        return await _context.Movies.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Movie>> GetMovie(int id)
    {
        var movie = await _context.Movies.FindAsync(id);

        if (movie == null)
        {
            return NotFound("La película no existe en tu base de datos.");
        }

        return movie;
    }



    [HttpPost("search/{title}")]
    public async Task<IActionResult> SearchAndSave(string title)
    {
        var omdbMovie = await _movieService.GetMovieFromApiAsync(title);

        if (omdbMovie == null || omdbMovie.Response == "False")
            return NotFound("No se encontró la película.");

        var newMovie = new Movie
        {
            Title = omdbMovie.Title,
            Year = int.Parse(omdbMovie.Year.Substring(0, 4)), 
            Director = omdbMovie.Director,
            Plot = omdbMovie.Plot,
            PosterUrl = omdbMovie.Poster,
            ImdbID = omdbMovie.imdbID
        };

        _context.Movies.Add(newMovie);
        await _context.SaveChangesAsync();

        return Ok(newMovie);
    }




    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMovie(int id)
    {
        var movie = await _context.Movies.FindAsync(id);

        if (movie == null)
        {
            return NotFound("No se encontró la película para borrar.");
        }

        _context.Movies.Remove(movie);
        await _context.SaveChangesAsync();

        return Ok($"La película '{movie.Title}' fue eliminada de CineTraker.");
    }




    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateMovie(int id, Movie movieActualizada)
    {
        if (id != movieActualizada.Id)
        {
            return BadRequest("El ID de la película no coincide.");
        }

        _context.Entry(movieActualizada).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!MovieExists(id))
            {
                return NotFound("La película ya no existe en la base de datos.");
            }
            else
            {
                throw;
            }
        }

        return Ok("Película actualizada correctamente.");
    }




    private bool MovieExists(int id)
    {
        return _context.Movies.Any(e => e.Id == id);
    }

}