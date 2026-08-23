using System.ComponentModel.DataAnnotations;

namespace SistemaAlmacen.Shared.DTOs.Caja;

/// <summary>
/// Request para abrir una caja.
/// </summary>
public class AbrirCajaRequest
{
    [Required(ErrorMessage = "El monto inicial es requerido.")]
    [Range(0, 999999999.99, ErrorMessage = "El monto inicial debe ser mayor o igual a cero.")]
    public decimal MontoInicial { get; set; }

    [Required(ErrorMessage = "El punto de venta es requerido.")]
    public int PuntoDeVentaId { get; set; }
}
