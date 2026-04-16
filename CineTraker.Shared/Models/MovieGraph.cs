using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CineTraker.Shared.Models
{
    public class MovieGraph
    {
        public List<MovieNode> Nodos { get; set; } = new();
        public List<MovieEdge> Aristas { get; set; } = new();
    }
}
