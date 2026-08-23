using Microsoft.EntityFrameworkCore;
using SistemaAlmacen.Data.Context;
using SistemaAlmacen.Data.Entities;
using SistemaAlmacen.Shared.DTOs;
using SistemaAlmacen.Shared.DTOs.Caja;
using SistemaAlmacen.Shared.Enums;

namespace SistemaAlmacen.Data.Repositories;

/// <summary>
/// Implementación del repositorio de cajas.
/// </summary>
public class CajaRepository : Repository<Caja>, ICajaRepository
{
    public CajaRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Caja?> GetCajaAbiertaAsync(int puntoDeVentaId)
    {
        return await _dbSet
            .Include(c => c.Movimientos)
            .FirstOrDefaultAsync(c =>
                c.PuntoDeVentaId == puntoDeVentaId &&
                c.Estado == EstadoCaja.Abierta);
    }

    public async Task<PaginatedResult<Caja>> GetHistorialCierresAsync(CierreFilter filter)
    {
        var query = _dbSet
            .Include(c => c.UsuarioApertura)
            .Include(c => c.UsuarioCierre)
            .Include(c => c.Movimientos)
            .Where(c => c.Estado == EstadoCaja.Cerrada);

        if (filter.FechaInicio.HasValue)
        {
            query = query.Where(c => c.FechaCierre >= filter.FechaInicio.Value);
        }

        if (filter.FechaFin.HasValue)
        {
            query = query.Where(c => c.FechaCierre <= filter.FechaFin.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(c => c.FechaCierre)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PaginatedResult<Caja>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task AddMovimientoAsync(int cajaId, CajaMovimiento movimiento)
    {
        movimiento.CajaId = cajaId;
        await _context.Set<CajaMovimiento>().AddAsync(movimiento);
    }
}
