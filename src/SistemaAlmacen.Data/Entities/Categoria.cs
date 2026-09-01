using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaAlmacen.Data.Entities;

/// <summary>
/// Representa una categoría que agrupa productos de forma lógica.
/// </summary>
public class Categoria
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Descripcion { get; set; }

    /// <summary>
    /// Margen de ganancia (porcentaje, ej: 40 = 40%) aplicable a los productos de la categoría.
    /// Si es null, se usa el margen por defecto del sistema. Un margen a nivel de producto lo sobrescribe.
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal? MargenGanancia { get; set; }

    [Required]
    public DateTime FechaCreacion { get; set; }

    // Navigation properties
    public ICollection<Producto> Productos { get; set; } = new List<Producto>();
}
