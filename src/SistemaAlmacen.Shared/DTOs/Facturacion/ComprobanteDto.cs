using SistemaAlmacen.Shared.Enums;

namespace SistemaAlmacen.Shared.DTOs.Facturacion;

/// <summary>
/// DTO de respuesta con datos de un comprobante fiscal.
/// </summary>
public class ComprobanteDto
{
    public int Id { get; set; }
    public int VentaId { get; set; }
    public int TipoComprobante { get; set; }
    public long NumeroComprobante { get; set; }
    public string CAE { get; set; } = string.Empty;
    public DateTime FechaVencimientoCAE { get; set; }
    public EstadoComprobante Estado { get; set; }
    public DateTime FechaEmision { get; set; }
}
