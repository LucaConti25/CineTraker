using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CineTraker.Shared.Models
{
    public class FiltroGenero
    {
        public string Nombre { get; set; }
        public bool Seleccionado { get; set; }

        public FiltroGenero(string nombre)
        {
            Nombre = nombre;
            Seleccionado = false;
        }
    }
}
