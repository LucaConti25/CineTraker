
using CineTraker.Data;
using CineTraker.Shared;
using CineTraker.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/[controller]")]
[ApiController]
public class MoviesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly MovieService _movieService;
    private readonly StreamingService _streamingService;

    public MoviesController(AppDbContext context, MovieService movieService, StreamingService streamingService)
    {
        _context = context;
        _movieService = movieService;
        _streamingService = streamingService;
    }


    [HttpGet]
    public async Task<ActionResult<IEnumerable<Movie>>> GetMovies()
    {
        return await _context.Movies
        .OrderBy(x => Guid.NewGuid())
        .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Movie>> GetMovie(int id)
    {
        var movie = await _context.Movies.FindAsync(id);

        if (movie == null)
        {
            return NotFound("La película no existe en tu base de datos.");
        }

        if (!string.IsNullOrEmpty(movie.ImdbID))
        {
            movie.Sources = await _streamingService.GetSourcesAsync(movie.ImdbID);
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

    [HttpGet("admin/seed")]
    public async Task<IActionResult> SeedData()
    {
        var ids = new List<string> { 
            "tt0111161", "tt0068646", "tt0468569", "tt0071562", "tt0050083", "tt0108052", "tt0167260", "tt0110912", "tt0060196", "tt0137523",
            "tt0120737", "tt0109830", "tt0080684", "tt1375666", "tt0167261", "tt0110357", "tt0075148", "tt0076759", "tt0034583", "tt0054215",
            "tt0114369", "tt0114814", "tt0102926", "tt0040522", "tt0118799", "tt0120815", "tt0050825", "tt0062622", "tt0021749", "tt0058331",
            "tt0099685", "tt0031381", "tt0081398", "tt0033467", "tt0107048", "tt0086190", "tt0105771", "tt0209144", "tt0047396", "tt0053125",
            "tt0050212", "tt0056119", "tt0032138", "tt0032603", "tt0055031", "tt0052357", "tt0035423", "tt0046912", "tt0057115", "tt0052026" 
        };
        var resultado = await _movieService.EjecutarCargaMasiva(ids);
        return Ok($"Proceso terminado. Se cargaron {resultado} películas nuevas.");
    }




    [HttpGet("smart-search/{title}")]
    public async Task<ActionResult<IEnumerable<Movie>>> SmartSearch(string title)
    {
        
        var localMovies = await _context.Movies
            .Where(m => m.Title.Contains(title))
            .ToListAsync();

        
        if (localMovies.Any())
        {
            return Ok(localMovies);
        }

        
        var omdbMovie = await _movieService.GetMovieFromApiAsync(title);

        if (omdbMovie != null && omdbMovie.Response == "True")
        {
            // 4. Verificamos si por ID de IMDb ya existe (doble check de seguridad)
            var existePorId = await _context.Movies.AnyAsync(m => m.ImdbID == omdbMovie.imdbID);

            if (!existePorId)
            {
                
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

                return Ok(new List<Movie> { newMovie });
            }
        }

        return Ok(new List<Movie>());
    }

}