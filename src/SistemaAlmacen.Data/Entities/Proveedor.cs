using System.ComponentModel.DataAnnotations;
using SistemaAlmacen.Shared.Enums;

namespace SistemaAlmacen.Data.Entities;

/// <summary>
/// Representa un proveedor de mercadería del comercio.
/// </summary>
public class Proveedor
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(150)]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>CUIT del proveedor. Opcional.</summary>
    [MaxLength(20)]
    public string? Cuit { get; set; }

    [Required]
    public CondicionIva CondicionIva { get; set; } = CondicionIva.ResponsableInscripto;

    [MaxLength(254)]
    [EmailAddress]
    public string? Email { get; set; }

    [MaxLength(30)]
    public string? Telefono { get; set; }

    [MaxLength(250)]
    public string? Direccion { get; set; }

    [Required]
    public bool Activo { get; set; } = true;

    [Required]
    public DateTime FechaCreacion { get; set; }

    [Required]
    public DateTime FechaModificacion { get; set; }

    // Navigation properties
    public ICollection<Compra> Compras { get; set; } = new List<Compra>();
}
