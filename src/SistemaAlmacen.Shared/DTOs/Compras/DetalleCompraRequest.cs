using System.ComponentModel.DataAnnotations;

namespace SistemaAlmacen.Shared.DTOs.Compras;

/// <summary>
/// Línea de una compra a registrar: producto, cantidad y costo unitario.
/// Opcionalmente, el nuevo precio de venta decidido para el producto.
/// </summary>
public class DetalleCompraRequest
{
    [Required(ErrorMessage = "El producto es obligatorio.")]
    public int ProductoId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor o igual a 1.")]
    public int Cantidad { get; set; }

    [Range(0, 999999999.99, ErrorMessage = "El costo unitario debe estar entre 0 y 999,999,999.99.")]
    public decimal CostoUnitario { get; set; }

    /// <summary>
    /// Nuevo precio de venta a aplicar al producto (opcional). Si es null, no se modifica el precio de venta.
    /// </summary>
    [Range(0.01, 999999999.99, ErrorMessage = "El precio de venta debe estar entre 0.01 y 999,999,999.99.")]
    public decimal? NuevoPrecioVenta { get; set; }
}
