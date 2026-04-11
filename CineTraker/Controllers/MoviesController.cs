
using Azure;
using CineTraker.Data;
using CineTraker.Services;
using CineTraker.Shared;
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
    public async Task<ActionResult<IEnumerable<Movie>>> GetMovies(
        [FromQuery] string? genre = null, 
        [FromQuery] double? minRating = null,
        [FromQuery] string? platform = null,
        [FromQuery] int skip = 0,        // <--- NUEVO
        [FromQuery] int take = 20 )
    {
        var query = _context.Movies
        .Include(m => m.Sources)
        .AsQueryable();

        // Si viene un género, filtramos
        if (!string.IsNullOrEmpty(genre))
        {
            query = query.Where(m => m.Genre != null && m.Genre.Contains(genre));
        }

        if (minRating.HasValue)
        {
            // OMDb a veces manda "N/A", lo filtramos para que no rompa el cast
            query = query.Where(m => m.ImdbRating != null &&
                                     m.ImdbRating != "N/A" &&
                                     Convert.ToDouble(m.ImdbRating) >= minRating.Value);
        }


        if (!string.IsNullOrEmpty(platform))
        {
            query = query.Where(m => m.Sources.Any(s => s.Name.Contains(platform)));
        }
        return await query
        .OrderByDescending(m => m.ImdbRating != null && m.ImdbRating != "N/A" ? m.ImdbRating : "0")
        .Skip(skip) // <--- SALTEA las que ya vimos
        .Take(take) // <--- TOMA solo la tanda que sigue
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



    [HttpPost]
    public async Task<ActionResult<Movie>> PostMovie(Movie movie)
    {
        // Este es el guardado real. Se dispara cuando el usuario confirma su elección en el modal.
        // Chequeamos el ImdbID para que no nos metan dos veces la misma peli.
        var existe = await _context.Movies.AnyAsync(m => m.ImdbID == movie.ImdbID);

        if (existe)
        {
            return BadRequest("La película ya está en tu catálogo.");
        }

        _context.Movies.Add(movie);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMovie), new { id = movie.Id }, movie);
    }




    [HttpPost("search/{title}")]
    public async Task<IActionResult> SearchAndSave(string title)
    {
        var omdbMovie = await _movieService.GetMovieFromApiAsync(title);

        if (omdbMovie == null || omdbMovie.Response == "False")
            return NotFound("No se encontró la película.");

        var runtimeLimpio = omdbMovie.Runtime?.Replace(" min", "").Replace("N/A", "0");

        var newMovie = new Movie
        {
            Title = omdbMovie.Title,
            Year = int.Parse(omdbMovie.Year.Substring(0, 4)), 
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
            "tt1302006", "tt0065421", "tt0368226", "tt5040012", "tt4010884", "tt7131622",
            "tt1375666", "tt114814", "tt0021749", "tt0058331", "tt0033467", "tt0107048",
            "tt0086190", "tt0105771", "tt0047396", "tt0053125", "tt0050212", "tt0056119",
            "tt0032603", "tt0035423", "tt0046912", "tt0052026", "tt32916440", "tt0110413",
            "tt0118332", "tt0042192", "tt0120338", "tt0082971", "tt0060665", "tt0113243",
            "tt0088763", "tt0092339", "tt0078718", "tt0087190", "tt0109721", "tt0119116",
            "tt0093870", "tt0043618", "tt0038306", "tt0090605", "tt0063350", "tt0101414",
            "tt0120663", "tt0059346", "tt0036855", "tt0054047", "tt0114319", "tt0099785",
            "tt0083944", "tt0035599", "tt0084302", "tt0073075", "tt0046064", "tt0082694",
            "tt0052561", "tt0106062", "tt0034022", "tt0042451", "tt0085443", "tt0035241",
            "tt0037145", "tt0067555", "tt0061512", "tt0117060", "tt0119698", "tt0024252",
            "tt0120689", "tt0067116", "tt0087332", "tt0065938", "tt0040741", "tt0110024",
            "tt0025914", "tt0035706", "tt0064505", "tt0050011", "tt0077416", "tt0080339",
            "tt0091223", "tt0051130", "tt0032712", "tt0118548", "tt0042556", "tt0031416",
            "tt0054331", "tt0041492", "tt0063665", "tt0045152", "tt0052618", "tt0112642",
            "tt0067355", "tt0087538", "tt0033235", "tt0084196", "tt0059113", "tt0073707",
            "tt0036443", "tt0060345", "tt0077975", "tt0101156", "tt0112775", "tt0073537",
            "tt0050613", "tt0032455", "tt0054226", "tt0071311", "tt0070511", "tt0113101",
            "tt0062060", "tt0076324", "tt0066922", "tt0082416", "tt6033368", "tt5311514",
            "tt0111495", "tt0109830", "tt0137523", "tt0050083", "tt0108052", "tt0110912",
            "tt0068646", "tt0071562", "tt0111161", "tt0120737", "tt0167260", "tt0167261",
            "tt0468569", "tt0080684", "tt0099685", "tt0073486", "tt0114369", "tt0118799",
            "tt0102926", "tt0038650", "tt0076759", "tt0120815", "tt0050825", "tt0062622",
            "tt0031381", "tt0081398", "tt0050212", "tt0032138", "tt0055031", "tt0052357",
            "tt0057115", "tt0082096", "tt0056592", "tt0064116", "tt0107290", "tt0081505",
            "tt0079470", "tt0091763", "tt0112471", "tt0103064", "tt0072684", "tt0053291",
            "tt0092099", "tt1745960", "tt0087182", "tt0970179", "tt5726616", "tt1099212",
            "tt0848228", "tt0076786", "tt0113277", "tt2404463"

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
        // Solo consultamos a la API externa para traer opciones al usuario.
        // No guardamos nada todavía para evitar llenar la DB con basura o versiones erróneas.
        var resultadosDesdeApi = await _movieService.SearchMoviesFromApiAsync(title);

        if (resultadosDesdeApi == null || !resultadosDesdeApi.Any())
        {
            return Ok(new List<Movie>());
        }

        // 2. Devolvemos la lista al Modal de Blazor para que el usuario elija
        return Ok(resultadosDesdeApi);
    }

    [HttpPost("save-by-id")]
    public async Task<ActionResult<Movie>> SaveMovieById([FromBody] string imdbId)
    {
        
        var existe = await _context.Movies.AnyAsync(m => m.ImdbID == imdbId);
        if (existe) return BadRequest("La película ya está en tu catálogo.");

        var peliCompleta = await _movieService.BuscarEnOmdbPorIdAsync(imdbId);

        if (peliCompleta == null)
            return NotFound("No se pudo obtener la información detallada.");

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

                    peliCompleta.Sources.Add(plataformaExistente);
                }
                else
                {
                    // Si no existe, la creamos
                    var nuevaPlataforma = new StreamingSource
                    {
                        Name = sourceApi.Name,
                        LogoUrl = sourceApi.LogoUrl,
                        Type = sourceApi.Type,
                        WebUrl = sourceApi.WebUrl
                    };

                    _context.StreamingSources.Add(nuevaPlataforma);
                    await _context.SaveChangesAsync();

                    peliCompleta.Sources.Add(nuevaPlataforma);
                }
            }
        }

        _context.Movies.Add(peliCompleta);
        await _context.SaveChangesAsync();

        return Ok(peliCompleta);
    }


    [HttpGet("admin/update-missing-data")]
    public async Task<IActionResult> UpdateMissingData()
    {
        // 1. Buscamos todas las películas que tengan el género o actores en NULL
        var peliculasIncompletas = await _context.Movies
            .Where(m => m.Genre == null || m.Actors == null || m.ImdbRating == null)
            .ToListAsync();

        int actualizadas = 0;

        foreach (var peli in peliculasIncompletas)
        {
            if (string.IsNullOrEmpty(peli.ImdbID)) continue;

            // 2. Usamos tu servicio para buscar la info COMPLETA por ID
            // (Asegurate que BuscarEnOmdbPorIdAsync mapee los nuevos campos)
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

            // Respetamos el límite de la API (delay de 200ms)
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
                                 ?? new StreamingSource { Name = p.Name, LogoUrl = p.LogoUrl };
                    peli.Sources.Add(dbPlat);
                }
                contador++;
            }
            await Task.Delay(200); // Para no banearte de la API
        }
        await _context.SaveChangesAsync();
        return Ok($"Se actualizaron {contador} películas.");
    }
}