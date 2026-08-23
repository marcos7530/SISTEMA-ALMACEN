using System.ComponentModel.DataAnnotations;
using SistemaAlmacen.Shared.Enums;

namespace SistemaAlmacen.Shared.DTOs.Usuarios;

/// <summary>
/// Request para crear un nuevo usuario.
/// </summary>
public class CreateUsuarioRequest
{
    [Required(ErrorMessage = "El nombre es requerido.")]
    [MaxLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electrónico es requerido.")]
    [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido.")]
    [MaxLength(254, ErrorMessage = "El correo electrónico no puede exceder 254 caracteres.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es requerida.")]
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    [MaxLength(50, ErrorMessage = "La contraseña no puede exceder 50 caracteres.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "El rol es requerido.")]
    public Rol Rol { get; set; }
}
