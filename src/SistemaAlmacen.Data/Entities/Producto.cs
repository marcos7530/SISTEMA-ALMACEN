using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaAlmacen.Data.Entities;

/// <summary>
/// Representa un artículo disponible para la venta.
/// </summary>
public class Producto
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? CodigoBarras { get; set; }

    [MaxLength(500)]
    public string? Descripcion { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Precio { get; set; }

    [Required]
    public int Stock { get; set; }

    [Required]
    public int CategoriaId { get; set; }

    [Required]
    public bool Activo { get; set; } = true;

    [Required]
    public DateTime FechaCreacion { get; set; }

    [Required]
    public DateTime FechaModificacion { get; set; }

    // Navigation properties
    [ForeignKey(nameof(CategoriaId))]
    public Categoria Categoria { get; set; } = null!;

    public ICollection<DetalleVenta> DetallesVenta { get; set; } = new List<DetalleVenta>();
}
