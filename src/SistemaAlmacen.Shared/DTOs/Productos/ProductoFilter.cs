namespace SistemaAlmacen.Shared.DTOs.Productos;

/// <summary>
/// Filtro para listado paginado de productos.
/// </summary>
public class ProductoFilter
{
    public string? SearchTerm { get; set; }
    public int? CategoriaId { get; set; }

    /// <summary>Filtra por estado: null = todos, true = solo activos, false = solo inactivos.</summary>
    public bool? Activo { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
