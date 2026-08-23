using System.ComponentModel.DataAnnotations;

namespace SistemaAlmacen.Shared.DTOs.Caja;

/// <summary>
/// Request para registrar un movimiento de caja (retiro o ingreso).
/// </summary>
public class MovimientoRequest
{
    [Required(ErrorMessage = "El monto es requerido.")]
    [Range(0.01, 999999999.99, ErrorMessage = "El monto debe ser mayor a cero.")]
    public decimal Monto { get; set; }

    [Required(ErrorMessage = "El motivo es requerido.")]
    [MaxLength(200, ErrorMessage = "El motivo no puede exceder 200 caracteres.")]
    public string Motivo { get; set; } = string.Empty;
}
