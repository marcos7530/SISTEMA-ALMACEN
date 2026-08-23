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

    public async Task<Comprobante?> GetByVentaIdAsync(int ventaId)
    {
        return await _dbSet
            .Include(c => c.Venta)
            .FirstOrDefaultAsync(c => c.VentaId == ventaId);
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
