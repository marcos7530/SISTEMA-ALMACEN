namespace SistemaAlmacen.Shared.DTOs.Proveedores;

/// <summary>
/// Filtros de búsqueda y paginación para el listado de proveedores.
/// </summary>
public class ProveedorFilter
{
    public string? SearchTerm { get; set; }

    /// <summary>Filtra por estado: null = todos, true = solo activos, false = solo inactivos.</summary>
    public bool? Activo { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
