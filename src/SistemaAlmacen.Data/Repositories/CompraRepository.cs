using Microsoft.EntityFrameworkCore;
using SistemaAlmacen.Data.Context;
using SistemaAlmacen.Data.Entities;
using SistemaAlmacen.Shared.DTOs;
using SistemaAlmacen.Shared.DTOs.Compras;

namespace SistemaAlmacen.Data.Repositories;

/// <summary>
/// Implementación del repositorio de compras.
/// </summary>
public class CompraRepository : Repository<Compra>, ICompraRepository
{
    public CompraRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Compra?> GetCompraConDetallesAsync(int id)
    {
        return await _dbSet
            .Include(c => c.Detalles)
                .ThenInclude(d => d.Producto)
            .Include(c => c.Proveedor)
            .Include(c => c.Usuario)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<PaginatedResult<Compra>> GetHistorialAsync(CompraFilter filter)
    {
        var query = _dbSet
            .Include(c => c.Proveedor)
            .Include(c => c.Detalles)
            .AsQueryable();

        if (filter.ProveedorId.HasValue)
        {
            query = query.Where(c => c.ProveedorId == filter.ProveedorId.Value);
        }

        if (filter.FechaInicio.HasValue)
        {
            query = query.Where(c => c.Fecha >= filter.FechaInicio.Value);
        }

        if (filter.FechaFin.HasValue)
        {
            var fechaFinInclusive = filter.FechaFin.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(c => c.Fecha <= fechaFinInclusive);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(c => c.Fecha)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PaginatedResult<Compra>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }
}
