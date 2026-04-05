
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
            "tt0133093", "tt0047478", "tt0317248", "tt0245429", "tt6751668", "tt2582802", "tt1675434", "tt0482571",
            "tt0407887", "tt0172495", "tt0253474", "tt0816692", "tt1853728", "tt1345836", "tt2380307", "tt1187043",
            "tt8503618", "tt0986264", "tt0405094", "tt7286456", "tt0910970", "tt0169547", "tt0364569", "tt0211915",
            "tt0022100", "tt0104346", "tt5074352", "tt0078788", "tt0078748", "tt0374887", "tt0097576", "tt0087843",
            "tt0208092", "tt0361748", "tt0071853", "tt1832382", "tt0086879", "tt0059578", "tt0096283", "tt0477348",
            "tt2106476", "tt1255953", "tt0119488", "tt0017136", "tt0042876", "tt0032976", "tt0758758", "tt0080678",
            "tt0457430", "tt0434409", "tt0053291", "tt0050986", "tt1205489", "tt0046268", "tt0120735", "tt0117951",
            "tt0015864", "tt0264464", "tt0120382", "tt0395169", "tt0167404", "tt0084787", "tt1305806", "tt2278388",
            "tt1895587", "tt1663202", "tt0074958", "tt0019254", "tt0113247", "tt0454876", "tt0087544", "tt0118845",
            "tt2338151", "tt1201607", "tt1028532", "tt0401383", "tt1392214", "tt4857264", "tt0087884", "tt1280558",
            "tt3783958", "tt0046911", "tt3501632", "tt0367110", "tt0060107", "tt0058946", "tt8613070", "tt0083922"

        };
        var resultado = await _movieService.EjecutarCargaMasiva(ids);
        return Ok($"Proceso terminado. Se cargaron {resultado} películas nuevas.");
    }

    [HttpGet("seed-single/{id}")]
    public async Task<IActionResult> SeedSingle(string id)
    {
        try
        {
            // Reutilizamos tu lógica de carga masiva pero para un solo ID
            var ids = new List<string> { id };
            var resultado = await _movieService.EjecutarCargaMasiva(ids);

            if (resultado > 0)
                return Ok(new { success = true, message = $"Cargada correctamente" });

            return Ok(new { success = false, message = "Ya existía en la base" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
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