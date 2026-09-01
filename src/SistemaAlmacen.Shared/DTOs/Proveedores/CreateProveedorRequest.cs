using System.ComponentModel.DataAnnotations;
using SistemaAlmacen.Shared.Enums;

namespace SistemaAlmacen.Shared.DTOs.Proveedores;

/// <summary>
/// Request para crear un proveedor.
/// </summary>
public class CreateProveedorRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(150, MinimumLength = 1, ErrorMessage = "El nombre debe tener entre 1 y 150 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "El CUIT no puede superar los 20 caracteres.")]
    public string? Cuit { get; set; }

    [Required(ErrorMessage = "La condición frente al IVA es obligatoria.")]
    public CondicionIva CondicionIva { get; set; } = CondicionIva.ResponsableInscripto;

    [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
    [StringLength(254, ErrorMessage = "El correo no puede superar los 254 caracteres.")]
    public string? Email { get; set; }

    [StringLength(30, ErrorMessage = "El teléfono no puede superar los 30 caracteres.")]
    public string? Telefono { get; set; }

    [StringLength(250, ErrorMessage = "La dirección no puede superar los 250 caracteres.")]
    public string? Direccion { get; set; }
}
