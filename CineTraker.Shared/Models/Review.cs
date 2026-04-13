using System.ComponentModel.DataAnnotations;

namespace CineTraker.Shared
{
    public class Review
    {
        [Key]
        public int Id { get; set; }

        [Range(1, 5)]
        public int Stars { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public int MovieId { get; set; }
        public string? UserId { get; set; }
    }
}