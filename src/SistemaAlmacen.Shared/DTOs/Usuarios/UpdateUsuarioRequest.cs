using System.ComponentModel.DataAnnotations;
using SistemaAlmacen.Shared.Enums;

namespace SistemaAlmacen.Shared.DTOs.Usuarios;

/// <summary>
/// Request para actualizar un usuario existente.
/// </summary>
public class UpdateUsuarioRequest
{
    [Required(ErrorMessage = "El nombre es requerido.")]
    [MaxLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electrónico es requerido.")]
    [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido.")]
    [MaxLength(254, ErrorMessage = "El correo electrónico no puede exceder 254 caracteres.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "El rol es requerido.")]
    public Rol Rol { get; set; }
}
