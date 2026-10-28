using Microsoft.EntityFrameworkCore;
using SistemaAlmacen.Data.Context;
using SistemaAlmacen.Data.Entities;
using SistemaAlmacen.Shared.DTOs;
using SistemaAlmacen.Shared.DTOs.Ventas;
using SistemaAlmacen.Shared.Enums;
using SistemaAlmacen.Shared.Common;

namespace SistemaAlmacen.Data.Repositories;

/// <summary>
/// Implementación del repositorio de ventas.
/// </summary>
public class VentaRepository : Repository<Venta>, IVentaRepository
{
    public VentaRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Venta?> GetVentaConDetallesAsync(int id)
    {
        return await _dbSet
            .Include(v => v.Detalles)
                .ThenInclude(d => d.Producto)
            .Include(v => v.Pagos)
                .ThenInclude(p => p.MedioPago)
            .Include(v => v.Usuario)
            .Include(v => v.Cliente)
            .Include(v => v.Comprobantes)
            .FirstOrDefaultAsync(v => v.Id == id);
    }

    public async Task<PaginatedResult<Venta>> GetHistorialAsync(VentaFilter filter)
    {
        var query = _dbSet
            .Include(v => v.Usuario)
            .Include(v => v.Cliente)
            .Include(v => v.Detalles)
            .AsQueryable();

        // Solo mostrar ventas confirmadas o pendientes de facturación en el historial (excluir borradores)
        query = query.Where(v => v.Estado != EstadoVenta.Borrador);

        if (filter.VendedorId.HasValue)
        {
            query = query.Where(v => v.UsuarioId == filter.VendedorId.Value);
        }

        // Las fechas de filtro llegan como día local de Argentina; se convierten al rango UTC
        // equivalente para comparar contra v.Fecha (almacenada en UTC). Ver ArgentinaTimeZone.
        var (inicioUtc, finUtc) = ArgentinaTimeZone.ToUtcRange(filter.FechaInicio, filter.FechaFin);
        query = query.Where(v => v.Fecha >= inicioUtc && v.Fecha <= finUtc);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(v => v.Fecha)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PaginatedResult<Venta>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }
}
