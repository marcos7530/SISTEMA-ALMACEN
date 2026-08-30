using Microsoft.EntityFrameworkCore;
using SistemaAlmacen.Data.Context;
using SistemaAlmacen.Data.Entities;
using SistemaAlmacen.Shared.DTOs;
using SistemaAlmacen.Shared.DTOs.Productos;

namespace SistemaAlmacen.Data.Repositories;

/// <summary>
/// Implementación del repositorio de productos.
/// </summary>
public class ProductoRepository : Repository<Producto>, IProductoRepository
{
    public ProductoRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<PaginatedResult<Producto>> GetActiveProductosAsync(ProductoFilter filter)
    {
        var query = _dbSet
            .Include(p => p.Categoria)
            .Where(p => p.Activo);

        if (filter.CategoriaId.HasValue)
        {
            query = query.Where(p => p.CategoriaId == filter.CategoriaId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.ToLower();
            query = query.Where(p =>
                p.Nombre.ToLower().Contains(term) ||
                (p.CodigoBarras != null && p.CodigoBarras.ToLower().Contains(term)) ||
                p.Categoria.Nombre.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(p => p.Nombre)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PaginatedResult<Producto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<PaginatedResult<Producto>> SearchAsync(string term, int page, int pageSize)
    {
        var termLower = term.ToLower();

        var query = _dbSet
            .Include(p => p.Categoria)
            .Where(p => p.Activo &&
                (p.Nombre.ToLower().Contains(termLower) ||
                 p.Categoria.Nombre.ToLower().Contains(termLower)));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(p => p.Nombre)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResult<Producto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
