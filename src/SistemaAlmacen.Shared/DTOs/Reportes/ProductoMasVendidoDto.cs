namespace SistemaAlmacen.Shared.DTOs.Reportes;

/// <summary>
/// DTO con datos de un producto más vendido.
/// </summary>
public class ProductoMasVendidoDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public int CantidadVendida { get; set; }
}
