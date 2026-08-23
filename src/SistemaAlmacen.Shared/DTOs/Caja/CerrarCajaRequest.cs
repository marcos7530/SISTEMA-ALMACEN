using System.ComponentModel.DataAnnotations;

namespace SistemaAlmacen.Shared.DTOs.Caja;

/// <summary>
/// Request para cerrar una caja.
/// </summary>
public class CerrarCajaRequest
{
    [Required(ErrorMessage = "El monto real de cierre es requerido.")]
    [Range(0, 999999999.99, ErrorMessage = "El monto de cierre debe ser mayor o igual a cero.")]
    public decimal MontoRealCierre { get; set; }
}
