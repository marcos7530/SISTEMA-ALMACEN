using System.ComponentModel.DataAnnotations;

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

    [Required]
    public DateTime FechaCreacion { get; set; }

    // Navigation properties
    public ICollection<Producto> Productos { get; set; } = new List<Producto>();
}
