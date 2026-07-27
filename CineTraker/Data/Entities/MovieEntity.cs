using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CineTraker.Data.Entities
{
    public class MovieEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;
        public int Year { get; set; }
        public string? Director { get; set; }
        public string? PosterUrl { get; set; }
        public string? Plot { get; set; }
        public string? ImdbID { get; set; }
        public string? Genre { get; set; }
        public int Runtime { get; set; }
        public string? Actors { get; set; }
        public string? Rated { get; set; }
        public float[]? PlotEmbedding { get; set; }
        public string? ImdbRating { get; set; }
        public List<StreamingSourceEntity>? Sources { get; set; } = new List<StreamingSourceEntity>();
    }
}
