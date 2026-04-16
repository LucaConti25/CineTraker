using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CineTraker.Shared.Models
{
    public class MovieEdge
    {
        public int SourceId { get; set; } // Id peli origen
        public int TargetId { get; set; } // Id peli destino
        public string Relacion { get; set; } = string.Empty; // Ej: "Mismo Director"
    }
}
