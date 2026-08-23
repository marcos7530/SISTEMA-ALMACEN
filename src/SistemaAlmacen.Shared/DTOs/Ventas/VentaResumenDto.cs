namespace SistemaAlmacen.Shared.DTOs.Ventas;

/// <summary>
/// DTO de resumen de venta para listados.
/// </summary>
public class VentaResumenDto
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public string Vendedor { get; set; } = string.Empty;
    public int CantidadProductos { get; set; }
    public decimal Total { get; set; }
}
