using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CineTraker.Shared
{
    public class RegisterRequest
    {
        [Required(ErrorMessage = "El nombre de usuario es obligatorio")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "El nombre de usuario debe tener entre 3 y 20 caracteres")]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Solo se permiten letras, números y guiones bajos")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "Formato de email inválido")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterResult
    {
        public bool Succeeded { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
