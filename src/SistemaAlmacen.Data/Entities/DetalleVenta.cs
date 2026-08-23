using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaAlmacen.Data.Entities;

/// <summary>
/// Representa una línea individual dentro de una venta (producto, cantidad, precio).
/// </summary>
public class DetalleVenta
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int VentaId { get; set; }

    [Required]
    public int ProductoId { get; set; }

    [Required]
    [Range(1, 10000)]
    public int Cantidad { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal PrecioUnitario { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Subtotal { get; set; }

    // Navigation properties
    [ForeignKey(nameof(VentaId))]
    public Venta Venta { get; set; } = null!;

    [ForeignKey(nameof(ProductoId))]
    public Producto Producto { get; set; } = null!;
}
