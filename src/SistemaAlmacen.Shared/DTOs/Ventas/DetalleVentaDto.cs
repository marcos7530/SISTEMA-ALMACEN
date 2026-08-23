namespace SistemaAlmacen.Shared.DTOs.Ventas;

/// <summary>
/// DTO de respuesta con datos de un detalle de venta.
/// </summary>
public class DetalleVentaDto
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public string ProductoNombre { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}
