namespace SistemaAlmacen.Shared.DTOs.Proveedores;

/// <summary>
/// Filtros de búsqueda y paginación para el listado de proveedores.
/// </summary>
public class ProveedorFilter
{
    public string? SearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
