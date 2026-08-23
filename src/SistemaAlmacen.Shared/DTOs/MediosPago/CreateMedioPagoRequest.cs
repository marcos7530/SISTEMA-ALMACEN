using System.ComponentModel.DataAnnotations;

namespace SistemaAlmacen.Shared.DTOs.MediosPago;

/// <summary>
/// Request para crear un nuevo medio de pago.
/// </summary>
public class CreateMedioPagoRequest
{
    [Required(ErrorMessage = "El nombre es requerido.")]
    [MinLength(3, ErrorMessage = "El nombre debe tener al menos 3 caracteres.")]
    [MaxLength(50, ErrorMessage = "El nombre no puede exceder 50 caracteres.")]
    public string Nombre { get; set; } = string.Empty;
}
