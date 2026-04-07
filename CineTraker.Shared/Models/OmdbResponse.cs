using System.Text.Json.Serialization;

namespace CineTraker.Shared
{
    public class OmdbResponse
    {
        public string Title { get; set; } = string.Empty;
        public string Year { get; set; } = string.Empty;
        public string Director { get; set; } = string.Empty;
        public string Poster { get; set; } = string.Empty;
        public string Plot { get; set; } = string.Empty;
        public string imdbID { get; set; } = string.Empty;

        public string? Genre { get; set; } = string.Empty;
        public string? Runtime { get; set; } = string.Empty;
        public string? Actors { get; set; } = string.Empty;
        public string? Rated { get; set; } = string.Empty;
        
        [JsonPropertyName("imdbRating")]
        public string imdbRating { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;
        
    }
}
