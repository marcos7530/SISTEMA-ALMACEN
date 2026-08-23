using SistemaAlmacen.Shared.Enums;

namespace SistemaAlmacen.Shared.DTOs.Ventas;

/// <summary>
/// DTO de respuesta con datos de una venta.
/// </summary>
public class VentaDto
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Total { get; set; }
    public EstadoVenta Estado { get; set; }
    public string VendedorNombre { get; set; } = string.Empty;
    public int VendedorId { get; set; }
}
