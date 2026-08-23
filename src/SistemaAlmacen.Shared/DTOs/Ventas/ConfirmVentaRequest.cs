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
}
