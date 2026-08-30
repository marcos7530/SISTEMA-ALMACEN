using Microsoft.EntityFrameworkCore;
using SistemaAlmacen.Data.Context;
using SistemaAlmacen.Data.Entities;

namespace SistemaAlmacen.Data.Repositories;

/// <summary>
/// Implementación del repositorio de movimientos de stock.
/// </summary>
public class MovimientoStockRepository : Repository<MovimientoStock>, IMovimientoStockRepository
{
    public MovimientoStockRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<MovimientoStock>> GetByProductoAsync(int productoId)
    {
        return await _dbSet
            .Where(m => m.ProductoId == productoId)
            .OrderByDescending(m => m.Fecha)
            .ToListAsync();
    }
}
