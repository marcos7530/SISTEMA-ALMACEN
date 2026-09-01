namespace SistemaAlmacen.Shared.DTOs.Clientes;

/// <summary>
/// Filtros de búsqueda y paginación para el listado de clientes.
/// </summary>
public class ClienteFilter
{
    public string? SearchTerm { get; set; }

    /// <summary>Si es true, devuelve solo clientes con cuenta corriente habilitada.</summary>
    public bool SoloCuentaCorriente { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
