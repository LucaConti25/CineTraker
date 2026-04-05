using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CineTraker.Shared.Models
{
    public class FiltroDecada
    {
        public string Etiqueta { get; set; } 
        public int AnioInicio { get; set; }  
        public bool Seleccionado { get; set; }

        public FiltroDecada(int anio)
        {
            AnioInicio = anio;
            Etiqueta = $"{anio} - {anio + 9}";
            Seleccionado = false;
        }
    }
}
