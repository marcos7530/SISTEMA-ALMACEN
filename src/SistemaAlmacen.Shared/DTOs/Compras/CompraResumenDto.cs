using SistemaAlmacen.Shared.Enums;

namespace SistemaAlmacen.Shared.DTOs.Compras;

/// <summary>
/// Resumen de una compra para listados de historial.
/// </summary>
public class CompraResumenDto
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public string ProveedorNombre { get; set; } = string.Empty;
    public string? NumeroComprobante { get; set; }
    public int CantidadItems { get; set; }
    public decimal Total { get; set; }
    public EstadoCompra Estado { get; set; }
}
