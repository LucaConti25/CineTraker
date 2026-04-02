using System.ComponentModel.DataAnnotations;

namespace CineTraker.Models
{
    public class Movie
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
    }
}