namespace SistemaAlmacen.Shared.DTOs.Productos;

/// <summary>
/// Filtro para listado paginado de productos.
/// </summary>
public class ProductoFilter
{
    public string? SearchTerm { get; set; }
    public int? CategoriaId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
