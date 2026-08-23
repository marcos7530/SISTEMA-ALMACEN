namespace SistemaAlmacen.Shared.DTOs.Facturacion;

/// <summary>
/// DTO con datos de una venta pendiente de facturación.
/// </summary>
public class VentaPendienteFacturacionDto
{
    public int VentaId { get; set; }
    public DateTime FechaVenta { get; set; }
    public decimal Total { get; set; }
    public string Error { get; set; } = string.Empty;
}
