using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SistemaAlmacen.Shared.Enums;

namespace SistemaAlmacen.Data.Entities;

/// <summary>
/// Representa una compra/ingreso de mercadería a un proveedor, compuesta por uno o más detalles.
/// </summary>
public class Compra
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ProveedorId { get; set; }

    [Required]
    public int UsuarioId { get; set; }

    [Required]
    public DateTime Fecha { get; set; }

    /// <summary>Número de remito o factura del proveedor. Opcional.</summary>
    [MaxLength(50)]
    public string? NumeroComprobante { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Total { get; set; }

    [Required]
    public EstadoCompra Estado { get; set; }

    [Required]
    public DateTime FechaCreacion { get; set; }

    // Navigation properties
    [ForeignKey(nameof(ProveedorId))]
    public Proveedor Proveedor { get; set; } = null!;

    [ForeignKey(nameof(UsuarioId))]
    public Usuario Usuario { get; set; } = null!;

    public ICollection<DetalleCompra> Detalles { get; set; } = new List<DetalleCompra>();
}
