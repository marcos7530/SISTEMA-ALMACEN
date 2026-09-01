namespace SistemaAlmacen.Shared.DTOs.Categorias;

/// <summary>
/// DTO de respuesta con datos de categoría.
/// </summary>
public class CategoriaDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }

    /// <summary>Margen de ganancia de la categoría (porcentaje). Null = usa margen por defecto.</summary>
    public decimal? MargenGanancia { get; set; }
    public DateTime FechaCreacion { get; set; }
}
