namespace SistemaAlmacen.Shared.DTOs.Usuarios;

/// <summary>
/// Solicitud para habilitar o inhabilitar un usuario (eliminación/reactivación lógica).
/// </summary>
public class CambiarEstadoUsuarioRequest
{
    /// <summary>true para habilitar el usuario, false para inhabilitarlo.</summary>
    public bool Activo { get; set; }
}
