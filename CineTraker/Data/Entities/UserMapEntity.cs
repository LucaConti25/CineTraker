using System.ComponentModel.DataAnnotations;

namespace CineTraker.Data.Entities
{
    public class UserMapEntity
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = "Nueva Expedición";
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string UserId { get; set; } = string.Empty;
        public string GraphJson { get; set; } = string.Empty;
        public int SeedMovieId { get; set; }
        public int TotalMovies { get; set; }
        public int WatchedMovies { get; set; }
        public MovieEntity? SeedMovie { get; set; }
    }
}
