using System.ComponentModel.DataAnnotations;
using SistemaAlmacen.Shared.Enums;

namespace SistemaAlmacen.Shared.DTOs.Clientes;

/// <summary>
/// Request para crear un cliente.
/// </summary>
public class CreateClienteRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(150, MinimumLength = 1, ErrorMessage = "El nombre debe tener entre 1 y 150 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "El documento no puede superar los 20 caracteres.")]
    public string? Documento { get; set; }

    [Required(ErrorMessage = "La condición frente al IVA es obligatoria.")]
    public CondicionIva CondicionIva { get; set; } = CondicionIva.ConsumidorFinal;

    [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
    [StringLength(254, ErrorMessage = "El correo no puede superar los 254 caracteres.")]
    public string? Email { get; set; }

    [StringLength(30, ErrorMessage = "El teléfono no puede superar los 30 caracteres.")]
    public string? Telefono { get; set; }

    [StringLength(250, ErrorMessage = "La dirección no puede superar los 250 caracteres.")]
    public string? Direccion { get; set; }

    public bool CuentaCorrienteHabilitada { get; set; }

    [Range(0, 999999999.99, ErrorMessage = "El límite de crédito debe estar entre 0 y 999.999.999,99.")]
    public decimal LimiteCredito { get; set; }
}
