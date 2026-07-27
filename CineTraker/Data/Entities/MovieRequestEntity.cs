using System.ComponentModel.DataAnnotations;
using CineTraker.Shared.Models;

namespace CineTraker.Data.Entities
{
    public class MovieRequestEntity
    {
        [Key]
        public int Id { get; set; }
        public string ImdbID { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Year { get; set; } = string.Empty;
        public string PosterUrl { get; set; } = string.Empty;
        public string RequestedByUserId { get; set; } = string.Empty;
        public string RequestedByUsername { get; set; } = string.Empty; 
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public RequestStatus Status { get; set; } = RequestStatus.Pending;
    }
}
