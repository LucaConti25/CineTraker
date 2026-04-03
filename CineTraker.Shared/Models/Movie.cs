using System.ComponentModel.DataAnnotations;

namespace CineTraker.Shared
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

        public string SafePosterUrl
        {
            get
            {
                if (string.IsNullOrWhiteSpace(PosterUrl) || PosterUrl == "N/A")
                {
                    
                    return "https://placehold.co/300x450?text=Sin+Poster";

                }

                return PosterUrl;
            }
        }
    }
}
