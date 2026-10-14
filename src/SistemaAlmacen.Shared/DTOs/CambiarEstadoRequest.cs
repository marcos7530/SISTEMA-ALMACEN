namespace SistemaAlmacen.Shared.DTOs;

/// <summary>
/// Solicitud genérica para habilitar o inhabilitar una entidad con eliminación lógica
/// (clientes, proveedores, etc.).
/// </summary>
public class CambiarEstadoRequest
{
    /// <summary>true para habilitar, false para inhabilitar.</summary>
    public bool Activo { get; set; }
}
