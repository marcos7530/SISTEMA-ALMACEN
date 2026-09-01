using SistemaAlmacen.Shared.Enums;

namespace SistemaAlmacen.Shared.DTOs.Proveedores;

/// <summary>
/// DTO de respuesta con datos de un proveedor.
/// </summary>
public class ProveedorDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Cuit { get; set; }
    public CondicionIva CondicionIva { get; set; }
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public bool Activo { get; set; }
}
