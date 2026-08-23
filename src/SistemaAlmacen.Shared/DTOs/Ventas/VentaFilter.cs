namespace SistemaAlmacen.Shared.DTOs.Ventas;

/// <summary>
/// Filtro para listado paginado de ventas.
/// </summary>
public class VentaFilter
{
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public int? VendedorId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
