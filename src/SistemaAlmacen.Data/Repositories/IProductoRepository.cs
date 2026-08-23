using SistemaAlmacen.Data.Entities;
using SistemaAlmacen.Shared.DTOs;
using SistemaAlmacen.Shared.DTOs.Productos;

namespace SistemaAlmacen.Data.Repositories;

/// <summary>
/// Repositorio específico para la entidad Producto.
/// </summary>
public interface IProductoRepository : IRepository<Producto>
{
    Task<PaginatedResult<Producto>> GetActiveProductosAsync(ProductoFilter filter);
    Task<PaginatedResult<Producto>> SearchAsync(string term, int page, int pageSize);
}
