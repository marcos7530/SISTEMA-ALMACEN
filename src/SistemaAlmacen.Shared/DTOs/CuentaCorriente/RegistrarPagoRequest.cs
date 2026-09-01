using System.ComponentModel.DataAnnotations;

namespace SistemaAlmacen.Shared.DTOs.CuentaCorriente;

/// <summary>
/// Request para registrar un cobro/pago en la cuenta corriente de un cliente.
/// </summary>
public class RegistrarPagoRequest
{
    [Range(0.01, 999999999.99, ErrorMessage = "El monto debe estar entre 0,01 y 999.999.999,99.")]
    public decimal Monto { get; set; }

    [StringLength(250, ErrorMessage = "La descripción no puede superar los 250 caracteres.")]
    public string? Descripcion { get; set; }
}
