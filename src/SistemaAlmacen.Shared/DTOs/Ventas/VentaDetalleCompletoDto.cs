using SistemaAlmacen.Shared.Enums;

namespace SistemaAlmacen.Shared.DTOs.Ventas;

/// <summary>
/// DTO con detalle completo de una venta incluyendo detalles y pagos.
/// </summary>
public class VentaDetalleCompletoDto
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Total { get; set; }
    public EstadoVenta Estado { get; set; }
    public string Vendedor { get; set; } = string.Empty;
    public List<DetalleVentaDto> Detalles { get; set; } = new();
    public List<VentaPagoDto> Pagos { get; set; } = new();
}
