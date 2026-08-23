using System.ComponentModel.DataAnnotations;

namespace SistemaAlmacen.Shared.DTOs.Ventas;

/// <summary>
/// Request para agregar un detalle (producto) a una venta.
/// </summary>
public class AddDetalleRequest
{
    [Required(ErrorMessage = "El producto es requerido.")]
    public int ProductoId { get; set; }

    [Required(ErrorMessage = "La cantidad es requerida.")]
    [Range(1, 10000, ErrorMessage = "La cantidad debe estar entre 1 y 10,000.")]
    public int Cantidad { get; set; }
}
