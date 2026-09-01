using Microsoft.EntityFrameworkCore;
using SistemaAlmacen.Data.Context;
using SistemaAlmacen.Data.Entities;
using SistemaAlmacen.Shared.Enums;

namespace SistemaAlmacen.Data.Repositories;

/// <summary>
/// Implementación del repositorio de comprobantes.
/// </summary>
public class ComprobanteRepository : Repository<Comprobante>, IComprobanteRepository
{
    public ComprobanteRepository(ApplicationDbContext context) : base(context)
    {
    }

    private static readonly int[] TiposNotaCredito =
    {
        (int)TipoComprobante.NotaCreditoA,
        (int)TipoComprobante.NotaCreditoB,
        (int)TipoComprobante.NotaCreditoC
    };

    public async Task<Comprobante?> GetByVentaIdAsync(int ventaId)
    {
        return await _dbSet
            .Include(c => c.Venta)
            .Where(c => c.VentaId == ventaId && !TiposNotaCredito.Contains(c.TipoComprobante))
            .OrderByDescending(c => c.Id)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Comprobante>> GetTodosPorVentaIdAsync(int ventaId)
    {
        return await _dbSet
            .Where(c => c.VentaId == ventaId)
            .OrderBy(c => c.Id)
            .ToListAsync();
    }

    public async Task<Comprobante?> GetNotaCreditoPorVentaIdAsync(int ventaId)
    {
        return await _dbSet
            .Include(c => c.Venta)
            .Where(c => c.VentaId == ventaId && TiposNotaCredito.Contains(c.TipoComprobante))
            .OrderByDescending(c => c.Id)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Comprobante>> GetPendientesAsync()
    {
        return await _dbSet
            .Include(c => c.Venta)
                .ThenInclude(v => v.Usuario)
            .Where(c => c.Estado == EstadoComprobante.Pendiente)
            .OrderBy(c => c.FechaEmision)
            .ToListAsync();
    }
}
