namespace SistemaAlmacen.Shared.DTOs.Compras;

/// <summary>
/// Filtros de búsqueda y paginación para el historial de compras.
/// </summary>
public class CompraFilter
{
    public int? ProveedorId { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
