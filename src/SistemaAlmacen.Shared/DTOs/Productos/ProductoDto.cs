namespace SistemaAlmacen.Shared.DTOs.Productos;

/// <summary>
/// DTO de respuesta con datos de producto.
/// </summary>
public class ProductoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? CodigoBarras { get; set; }
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public decimal PrecioCosto { get; set; }

    /// <summary>Margen de ganancia propio del producto (porcentaje). Null = hereda de categoría/default.</summary>
    public decimal? MargenGanancia { get; set; }

    /// <summary>Margen efectivo aplicado (producto > categoría > default), en porcentaje.</summary>
    public decimal MargenEfectivo { get; set; }
    public int Stock { get; set; }
    public string CategoriaNombre { get; set; } = string.Empty;
    public int CategoriaId { get; set; }
    public bool Activo { get; set; }
}
