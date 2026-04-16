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
    }
}
