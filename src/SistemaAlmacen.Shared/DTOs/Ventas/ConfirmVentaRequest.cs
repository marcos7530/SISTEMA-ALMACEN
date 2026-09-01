using System.ComponentModel.DataAnnotations;

namespace SistemaAlmacen.Shared.DTOs.Ventas;

/// <summary>
/// Request para confirmar una venta con los pagos asociados.
/// </summary>
public class ConfirmVentaRequest
{
    [Required(ErrorMessage = "Debe especificar al menos un medio de pago.")]
    [MinLength(1, ErrorMessage = "Debe especificar al menos un medio de pago.")]
    public List<PagoItemRequest> Pagos { get; set; } = new();

    /// <summary>
    /// Cliente asociado a la venta. Requerido si se usa el medio de pago Cuenta Corriente.
    /// </summary>
    public int? ClienteId { get; set; }
}
