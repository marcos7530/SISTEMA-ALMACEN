using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaAlmacen.Data.Entities;

/// <summary>
/// Representa una línea de una compra: producto ingresado, cantidad y costo unitario.
/// </summary>
public class DetalleCompra
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CompraId { get; set; }

    [Required]
    public int ProductoId { get; set; }

    [Required]
    public int Cantidad { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal CostoUnitario { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Subtotal { get; set; }

    // Navigation properties
    [ForeignKey(nameof(CompraId))]
    public Compra Compra { get; set; } = null!;

    [ForeignKey(nameof(ProductoId))]
    public Producto Producto { get; set; } = null!;
}
