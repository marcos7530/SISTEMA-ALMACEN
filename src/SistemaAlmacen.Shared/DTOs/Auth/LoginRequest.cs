using System.ComponentModel.DataAnnotations;

namespace SistemaAlmacen.Shared.DTOs.Auth;

/// <summary>
/// Request para iniciar sesión.
/// </summary>
public class LoginRequest
{
    [Required(ErrorMessage = "El correo electrónico es requerido.")]
    [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido.")]
    [MaxLength(254, ErrorMessage = "El correo electrónico no puede exceder 254 caracteres.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es requerida.")]
    public string Password { get; set; } = string.Empty;
}
