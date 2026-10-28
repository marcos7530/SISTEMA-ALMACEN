using Microsoft.EntityFrameworkCore;
using SistemaAlmacen.Data.Context;
using SistemaAlmacen.Data.Entities;
using SistemaAlmacen.Shared.DTOs;
using SistemaAlmacen.Shared.DTOs.Auditoria;
using SistemaAlmacen.Shared.Common;

namespace SistemaAlmacen.Data.Repositories;

/// <summary>
/// Implementación del repositorio de auditoría.
/// </summary>
public class AuditoriaRepository : Repository<AuditoriaLog>, IAuditoriaRepository
{
    public AuditoriaRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<PaginatedResult<AuditoriaLog>> GetHistorialAsync(AuditoriaFilter filter)
    {
        var query = _dbSet
            .Include(a => a.Usuario)
            .AsQueryable();

        if (filter.UsuarioId.HasValue)
        {
            query = query.Where(a => a.UsuarioId == filter.UsuarioId.Value);
        }

        // Las fechas de filtro llegan como día local de Argentina; se convierten al rango UTC
        // equivalente para comparar contra a.Fecha (almacenada en UTC). Ver ArgentinaTimeZone.
        var (inicioUtc, finUtc) = ArgentinaTimeZone.ToUtcRange(filter.FechaInicio, filter.FechaFin);
        query = query.Where(a => a.Fecha >= inicioUtc && a.Fecha <= finUtc);

        if (filter.TipoOperacion.HasValue)
        {
            query = query.Where(a => a.TipoOperacion == filter.TipoOperacion.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.EntidadAfectada))
        {
            query = query.Where(a => a.EntidadAfectada == filter.EntidadAfectada);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(a => a.Fecha)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PaginatedResult<AuditoriaLog>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }
}
