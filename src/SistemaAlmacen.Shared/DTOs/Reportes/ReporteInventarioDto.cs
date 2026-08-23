namespace SistemaAlmacen.Shared.DTOs.Reportes;

/// <summary>
/// DTO con datos de inventario para reportes.
/// </summary>
public class ReporteInventarioDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public int Stock { get; set; }
    public decimal Precio { get; set; }
    public bool Activo { get; set; }
}
