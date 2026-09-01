using SistemaAlmacen.Data.Entities;
using SistemaAlmacen.Shared.DTOs;
using SistemaAlmacen.Shared.DTOs.Compras;

namespace SistemaAlmacen.Data.Repositories;

/// <summary>
/// Repositorio específico para la entidad Compra.
/// </summary>
public interface ICompraRepository : IRepository<Compra>
{
    /// <summary>Obtiene una compra con sus detalles, productos y proveedor.</summary>
    Task<Compra?> GetCompraConDetallesAsync(int id);

    /// <summary>Historial paginado de compras con filtros por proveedor y fecha.</summary>
    Task<PaginatedResult<Compra>> GetHistorialAsync(CompraFilter filter);
}
