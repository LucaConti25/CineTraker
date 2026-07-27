using System.Security.Claims;
using Azure;
using CineTraker.Data;
using CineTraker.Data.Entities;
using CineTraker.Services;
using CineTraker.Shared;
using CineTraker.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize]
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

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Movie>>> GetMovies(
        [FromQuery] string? search = null,
        [FromQuery] string? genre = null, 
        [FromQuery] double? minRating = null,
        [FromQuery] string? platform = null,
        [FromQuery] int skip = 0,       
        [FromQuery] int take = 20 )
    {
        var query = _context.Movies
        .Include(m => m.Sources)
        .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(m => m.Title.Contains(search));
        }

        if (!string.IsNullOrEmpty(genre))
        {
            query = query.Where(m => m.Genre != null && m.Genre.Contains(genre));
        }

        if (minRating.HasValue)
        {
            query = query.Where(m => m.ImdbRating != null &&
                                     m.ImdbRating != "N/A" &&
                                     Convert.ToDouble(m.ImdbRating) >= minRating.Value);
        }

        if (!string.IsNullOrEmpty(platform))
        {
            query = query.Where(m => m.Sources.Any(s => s.Name.Contains(platform)));
        }
        
        var movies = await query
        .OrderByDescending(m => m.ImdbRating != null && m.ImdbRating != "N/A" ? m.ImdbRating : "0")
        .Skip(skip) 
        .Take(take) 
        .ToListAsync();

        return Ok(movies.Select(m => m.ToDto()).ToList());
    }


    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<ActionResult<Movie>> GetMovie(int id)
    {
        var movie = await _context.Movies.Include(m => m.Sources).FirstOrDefaultAsync(m => m.Id == id);

        if (movie == null)
        {
            return NotFound("La película no existe en tu base de datos.");
        }

        if (!string.IsNullOrEmpty(movie.ImdbID))
        {
            var sources = await _streamingService.GetSourcesAsync(movie.ImdbID);
            if (sources != null) 
            {
                movie.Sources = sources.Select(s => s.ToEntity()).Where(s => s != null).Cast<StreamingSourceEntity>().ToList();
            }
        }

        return Ok(movie.ToDto());
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("reject-request/{id}")]
    public async Task<IActionResult> RejectRequest(int id)
    {
        var request = await _context.MovieRequests.FindAsync(id);
        if (request == null) return NotFound("La solicitud no existe.");

        var solicitudesRelacionadas = await _context.MovieRequests
            .Where(r => r.ImdbID == request.ImdbID && r.Status == RequestStatus.Pending)
            .ToListAsync();

        foreach (var req in solicitudesRelacionadas)
        {
            req.Status = RequestStatus.Rejected;
        }

        await _context.SaveChangesAsync();
        return Ok("Solicitudes rechazadas y archivadas.");
    }

    [HttpPost]
    public async Task<ActionResult<Movie>> PostMovie(Movie movie)
    {
        var existe = await _context.Movies.AnyAsync(m => m.ImdbID == movie.ImdbID);

        if (existe)
        {
            return BadRequest("La película ya está en tu catálogo.");
        }

        var entity = movie.ToEntity();
        if (entity == null) return BadRequest();

        _context.Movies.Add(entity);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMovie), new { id = entity.Id }, entity.ToDto());
    }


    [HttpPost("search/{title}")]
    public async Task<IActionResult> SearchAndSave(string title)
    {
        var omdbMovie = await _movieService.GetMovieFromApiAsync(title);

        if (omdbMovie == null || omdbMovie.Response == "False")
            return NotFound("No se encontró la película.");

        var runtimeLimpio = omdbMovie.Runtime?.Replace(" min", "").Replace("N/A", "0");

        var newMovie = new MovieEntity
        {
            Title = omdbMovie.Title,
            Year = int.TryParse(omdbMovie.Year.Substring(0, 4), out int y) ? y : 0, 
            Director = omdbMovie.Director,
            Plot = omdbMovie.Plot,
            PosterUrl = omdbMovie.Poster,
            ImdbID = omdbMovie.imdbID,
            Genre = omdbMovie.Genre,
            Runtime = int.TryParse(runtimeLimpio, out int r) ? r : 0,
            Actors =omdbMovie.Actors,
            Rated = omdbMovie.Rated,
            ImdbRating = omdbMovie.imdbRating
        };

        _context.Movies.Add(newMovie);
        await _context.SaveChangesAsync();

        return Ok(newMovie.ToDto());
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

        var entity = movieActualizada.ToEntity();
        if (entity == null) return BadRequest();

        _context.Entry(entity).State = EntityState.Modified;

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
            "tt1302006", "tt0065421", "tt0368226", "tt5040012", "tt4010884", "tt7131622",
            "tt1375666", "tt114814", "tt0021749", "tt0058331", "tt0033467", "tt0107048",
            "tt0086190", "tt0105771", "tt0047396", "tt0053125", "tt0050212", "tt0056119"
        };
        var resultado = await _movieService.EjecutarCargaMasiva(ids);
        return Ok($"Proceso terminado. Se cargaron {resultado} películas nuevas.");
    }

    [HttpGet("seed-single/{id}")]
    public async Task<IActionResult> SeedSingle(string id)
    {
        try
        {
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

    [AllowAnonymous]
    [HttpGet("smart-search/{title}")]
    public async Task<ActionResult<IEnumerable<Movie>>> SmartSearch(string title)
    {
        var resultadosDesdeApi = await _movieService.SearchMoviesFromApiAsync(title);

        if (resultadosDesdeApi == null || !resultadosDesdeApi.Any())
        {
            return Ok(new List<Movie>());
        }

        return Ok(resultadosDesdeApi);
    }

    [HttpPost("save-by-id")]
    public async Task<ActionResult<Movie>> SaveMovieById([FromBody] string imdbId)
    {
        var existe = await _context.Movies.AnyAsync(m => m.ImdbID == imdbId);
        if (existe) return BadRequest("La película ya está en tu catálogo.");

        var peliDto = await _movieService.BuscarEnOmdbPorIdAsync(imdbId);
        if (peliDto == null) return NotFound("No se pudo obtener la información detallada.");

        var entity = peliDto.ToEntity();
        if (entity == null) return BadRequest();

        var plataformasApi = await _streamingService.GetSourcesAsync(imdbId);

        if (plataformasApi != null && plataformasApi.Any())
        {
            foreach (var sourceApi in plataformasApi)
            {
                var plataformaExistente = await _context.StreamingSources
         .FirstOrDefaultAsync(s => s.Name == sourceApi.Name);

                if (plataformaExistente != null)
                {
                    plataformaExistente.WebUrl = sourceApi.WebUrl;
                    entity.Sources.Add(plataformaExistente);
                }
                else
                {
                    var nuevaPlataforma = new StreamingSourceEntity
                    {
                        Name = sourceApi.Name,
                        LogoUrl = sourceApi.LogoUrl,
                        Type = sourceApi.Type,
                        WebUrl = sourceApi.WebUrl
                    };

                    _context.StreamingSources.Add(nuevaPlataforma);
                    await _context.SaveChangesAsync();

                    entity.Sources.Add(nuevaPlataforma);
                }
            }
        }

        _context.Movies.Add(entity);
        await _context.SaveChangesAsync();

        return Ok(entity.ToDto());
    }

    [HttpGet("admin/update-missing-data")]
    public async Task<IActionResult> UpdateMissingData()
    {
        var peliculasIncompletas = await _context.Movies
            .Where(m => m.Genre == null || m.Actors == null || m.ImdbRating == null)
            .ToListAsync();

        int actualizadas = 0;

        foreach (var peli in peliculasIncompletas)
        {
            if (string.IsNullOrEmpty(peli.ImdbID)) continue;

            var infoCompleta = await _movieService.BuscarEnOmdbPorIdAsync(peli.ImdbID);

            if (infoCompleta != null)
            {
                peli.Genre = infoCompleta.Genre;
                peli.Runtime = infoCompleta.Runtime;
                peli.Actors = infoCompleta.Actors;
                peli.Rated = infoCompleta.Rated;
                peli.ImdbRating = infoCompleta.ImdbRating;

                actualizadas++;
            }
            await Task.Delay(200);
        }

        await _context.SaveChangesAsync();
        return Ok($"Se actualizaron {actualizadas} películas con datos nuevos.");
    }

    [HttpGet("admin/fix-platforms")]
    public async Task<IActionResult> FixPlatforms()
    {
        var peliculasSinPlataforma = await _context.Movies
            .Include(m => m.Sources)
            .Where(m => !m.Sources.Any())
            .ToListAsync();

        int contador = 0;
        foreach (var peli in peliculasSinPlataforma)
        {
            var plataformas = await _streamingService.GetSourcesAsync(peli.ImdbID);
            if (plataformas != null)
            {
                foreach (var p in plataformas)
                {
                    var dbPlat = await _context.StreamingSources.FirstOrDefaultAsync(x => x.Name == p.Name)
                                 ?? new StreamingSourceEntity { Name = p.Name, LogoUrl = p.LogoUrl };
                    peli.Sources.Add(dbPlat);
                }
                contador++;
            }
            await Task.Delay(200);
        }
        await _context.SaveChangesAsync();
        return Ok($"Se actualizaron {contador} películas.");
    }


    [HttpPost("request-movie")]
    public async Task<IActionResult> RequestMovie([FromBody] MovieRequest request)
    {
        var existeEnCatalogo = await _context.Movies.AnyAsync(m => m.ImdbID == request.ImdbID);
        if (existeEnCatalogo) return BadRequest("Esta película ya forma parte del catálogo.");

        var solicitudExistente = await _context.MovieRequests
            .AnyAsync(r => r.ImdbID == request.ImdbID && r.Status == RequestStatus.Pending);

        if (solicitudExistente) return BadRequest("Ya existe una solicitud pendiente para esta película.");

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var username = User.Identity?.Name;

        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var entity = request.ToEntity();
        if (entity == null) return BadRequest();

        entity.RequestedByUserId = userId;
        entity.RequestedByUsername = username ?? "Usuario desconocido";
        entity.RequestedAt = DateTime.UtcNow;
        entity.Status = RequestStatus.Pending;

        _context.MovieRequests.Add(entity);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Solicitud enviada con éxito. El administrador la revisará pronto." });
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("requests")]
    public async Task<ActionResult<IEnumerable<MovieRequest>>> GetPendingRequests()
    {
        var reqs = await _context.MovieRequests
            .Where(r => r.Status == RequestStatus.Pending)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync();
            
        return Ok(reqs.Select(r => r.ToDto()).ToList());
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("approve-request/{id}")]
    public async Task<IActionResult> ApproveRequest(int id)
    {
        var request = await _context.MovieRequests.FindAsync(id);
        if (request == null) return NotFound("La solicitud no existe.");

        var saveResult = await SaveMovieById(request.ImdbID);

        if (saveResult.Result is OkObjectResult || saveResult.Result is BadRequestObjectResult)
        {
            var solicitudesRelacionadas = await _context.MovieRequests
                .Where(r => r.ImdbID == request.ImdbID && r.Status == RequestStatus.Pending)
                .ToListAsync();

            foreach (var req in solicitudesRelacionadas)
            {
                req.Status = RequestStatus.Approved;
            }

            await _context.SaveChangesAsync();
            return Ok("Película aprobada y solicitudes actualizadas.");
        }

        return BadRequest("Hubo un error al procesar la película desde la API externa.");
    }
}