using Microsoft.EntityFrameworkCore;
using SistemaAlmacen.Data.Context;
using SistemaAlmacen.Data.Entities;
using SistemaAlmacen.Shared.DTOs;
using SistemaAlmacen.Shared.DTOs.Proveedores;

namespace SistemaAlmacen.Data.Repositories;

/// <summary>
/// Implementación del repositorio de proveedores.
/// </summary>
public class ProveedorRepository : Repository<Proveedor>, IProveedorRepository
{
    public ProveedorRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<PaginatedResult<Proveedor>> GetActivosAsync(ProveedorFilter filter)
    {
        var query = _dbSet.Where(p => p.Activo);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.ToLower();
            query = query.Where(p =>
                p.Nombre.ToLower().Contains(term) ||
                (p.Cuit != null && p.Cuit.ToLower().Contains(term)) ||
                (p.Email != null && p.Email.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(p => p.Nombre)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PaginatedResult<Proveedor>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<bool> ExisteCuitActivoAsync(string cuit, int? excluirProveedorId = null)
    {
        return await _dbSet.AnyAsync(p =>
            p.Cuit == cuit &&
            p.Activo &&
            (excluirProveedorId == null || p.Id != excluirProveedorId.Value));
    }

    public async Task<bool> TieneComprasAsync(int proveedorId)
    {
        return await _context.Set<Compra>().AnyAsync(c => c.ProveedorId == proveedorId);
    }
}
