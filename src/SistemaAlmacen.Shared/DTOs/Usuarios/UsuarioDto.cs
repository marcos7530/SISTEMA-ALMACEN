using SistemaAlmacen.Shared.Enums;

namespace SistemaAlmacen.Shared.DTOs.Usuarios;

/// <summary>
/// DTO de respuesta con datos de usuario.
/// </summary>
public class UsuarioDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Rol Rol { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
}
