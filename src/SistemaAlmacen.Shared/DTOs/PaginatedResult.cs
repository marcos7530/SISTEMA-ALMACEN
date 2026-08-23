namespace SistemaAlmacen.Shared.DTOs;

/// <summary>
/// Resultado paginado genérico para listados con paginación.
/// </summary>
/// <typeparam name="T">Tipo de los elementos en la lista.</typeparam>
public class PaginatedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}
