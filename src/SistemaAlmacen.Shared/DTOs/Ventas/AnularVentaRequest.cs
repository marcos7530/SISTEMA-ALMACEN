using System.ComponentModel.DataAnnotations;

namespace SistemaAlmacen.Shared.DTOs.Ventas;

/// <summary>
/// Request para anular una venta confirmada.
/// </summary>
public class AnularVentaRequest
{
    [Required(ErrorMessage = "Debe indicar el motivo de la anulación.")]
    [StringLength(250, MinimumLength = 3, ErrorMessage = "El motivo debe tener entre 3 y 250 caracteres.")]
    public string Motivo { get; set; } = string.Empty;

    /// <summary>
    /// Si es true y la venta tiene un comprobante fiscal emitido, se emite una nota de crédito AFIP.
    /// </summary>
    public bool EmitirNotaCredito { get; set; } = true;
}
