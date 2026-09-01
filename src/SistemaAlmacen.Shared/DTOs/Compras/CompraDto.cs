using SistemaAlmacen.Shared.Enums;

namespace SistemaAlmacen.Shared.DTOs.Compras;

/// <summary>
/// DTO con el detalle completo de una compra.
/// </summary>
public class CompraDto
{
    public int Id { get; set; }
    public int ProveedorId { get; set; }
    public string ProveedorNombre { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string? NumeroComprobante { get; set; }
    public decimal Total { get; set; }
    public EstadoCompra Estado { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public List<DetalleCompraDto> Detalles { get; set; } = new();
}

/// <summary>
/// Línea de detalle de una compra.
/// </summary>
public class DetalleCompraDto
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public string ProductoNombre { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
    public decimal Subtotal { get; set; }
}
