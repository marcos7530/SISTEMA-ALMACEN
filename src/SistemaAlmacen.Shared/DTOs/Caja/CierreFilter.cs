namespace SistemaAlmacen.Shared.DTOs.Caja;

/// <summary>
/// Filtro para listado paginado de cierres de caja.
/// </summary>
public class CierreFilter
{
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
