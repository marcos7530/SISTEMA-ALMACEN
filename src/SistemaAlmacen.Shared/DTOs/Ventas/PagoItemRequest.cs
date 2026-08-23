using System.ComponentModel.DataAnnotations;

namespace SistemaAlmacen.Shared.DTOs.Ventas;

/// <summary>
/// Representa un pago individual dentro de una confirmación de venta.
/// </summary>
public class PagoItemRequest
{
    [Required(ErrorMessage = "El medio de pago es requerido.")]
    public int MedioPagoId { get; set; }

    [Required(ErrorMessage = "El monto es requerido.")]
    [Range(0.01, 999999999.99, ErrorMessage = "El monto debe ser mayor a cero.")]
    public decimal Monto { get; set; }
}
