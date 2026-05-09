using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CineTraker.Shared.Models
{
    public class MovieRequest
    {
        public int Id { get; set; }

        // Datos básicos de la peli para mostrar en el panel
        public string ImdbID { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Year { get; set; } = string.Empty;
        public string PosterUrl { get; set; } = string.Empty;

        // Datos del usuario que la pide
        public string RequestedByUserId { get; set; } = string.Empty;
        public string RequestedByUsername { get; set; } = string.Empty; 
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public RequestStatus Status { get; set; } = RequestStatus.Pending;
    }

    public enum RequestStatus
    {
        Pending,   // 0: Pendiente de revisión
        Approved,  // 1: Aprobada y agregada
        Rejected   // 2: Rechazada 
    }
}

