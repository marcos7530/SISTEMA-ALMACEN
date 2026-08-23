using System.ComponentModel.DataAnnotations;

namespace SistemaAlmacen.Shared.DTOs.MediosPago;

/// <summary>
/// Request para actualizar un medio de pago existente.
/// </summary>
public class UpdateMedioPagoRequest
{
    [Required(ErrorMessage = "El nombre es requerido.")]
    [MinLength(3, ErrorMessage = "El nombre debe tener al menos 3 caracteres.")]
    [MaxLength(50, ErrorMessage = "El nombre no puede exceder 50 caracteres.")]
    public string Nombre { get; set; } = string.Empty;
}
