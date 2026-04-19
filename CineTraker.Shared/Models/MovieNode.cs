using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CineTraker.Shared.Models
{
    public class MovieNode
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string PosterUrl { get; set; } = string.Empty;
        public string Grupo { get; set; } = string.Empty; // Ej: "Director" o "Genero"

        public bool IsWatched { get; set; } // Para el Blanco y Negro
        public double? Score { get; set; }    // Para saber si ramifica o poda
        public double? X { get; set; }        // Para guardar la posición (Snapshot)
        public double? Y { get; set; }        // Para guardar la posición (Snapshot)
    }
}
