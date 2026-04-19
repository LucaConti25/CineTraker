using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CineTraker.Shared.Models
{
    public class UserMap
    {
        public int Id { get; set; }
        public string Name { get; set; } = "Nueva Expedición";
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public string UserId { get; set; } = string.Empty; // Relación con Identity

        public string GraphJson { get; set; } = string.Empty;
        public int SeedMovieId { get; set; }
        public int TotalMovies { get; set; }
        public int WatchedMovies { get; set; }
    }
}
