using System.ComponentModel.DataAnnotations;

namespace InnovaParfumPOS.Backend.DTOs;

public class UserDto
{
    public int? IdUsuario { get; set; }
    public int? IdEmpleado { get; set; }
    public int? IdPersona { get; set; }

    [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
    public string Username { get; set; } = string.Empty;

    // Password es requerido solo en creación. En edición es opcional.
    public string? Password { get; set; }

    public List<int> SelectedModuloIds { get; set; } = new();

    [Required(ErrorMessage = "El rol es obligatorio.")]
    public int IdRol { get; set; }

    public string? NombreRol { get; set; }
    
    public int IdEstado { get; set; } = 1;

    // Datos de Persona
    [Required(ErrorMessage = "El primer nombre es obligatorio.")]
    public string PrimerNombre { get; set; } = string.Empty;

    public string? SegundoNombre { get; set; }

    [Required(ErrorMessage = "El primer apellido es obligatorio.")]
    public string PrimerApellido { get; set; } = string.Empty;

    public string? SegundoApellido { get; set; }

    [Required(ErrorMessage = "El tipo de identificación es obligatorio.")]
    public int IdTipoId { get; set; }

    [Required(ErrorMessage = "El número de identificación es obligatorio.")]
    public string NumIdentificacion { get; set; } = string.Empty;

    public int? IdGenero { get; set; }

    public string? Telefono { get; set; }

    [EmailAddress(ErrorMessage = "Formato de correo inválido.")]
    public string? Email { get; set; }

    public string? Direccion { get; set; }

    public string NombreCompleto => $"{PrimerNombre} {PrimerApellido}".Trim();
}

