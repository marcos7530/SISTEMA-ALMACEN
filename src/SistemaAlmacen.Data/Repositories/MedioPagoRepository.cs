using Microsoft.EntityFrameworkCore;
using SistemaAlmacen.Data.Context;
using SistemaAlmacen.Data.Entities;

namespace SistemaAlmacen.Data.Repositories;

/// <summary>
/// Implementación del repositorio de medios de pago.
/// </summary>
public class MedioPagoRepository : Repository<MedioPago>, IMedioPagoRepository
{
    public MedioPagoRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<MedioPago>> GetActivosAsync()
    {
        return await _dbSet
            .Where(mp => mp.Activo)
            .OrderBy(mp => mp.Nombre)
            .ToListAsync();
    }

    public async Task<MedioPago?> GetByNameAsync(string nombre)
    {
        return await _dbSet
            .FirstOrDefaultAsync(mp => mp.Nombre.ToLower() == nombre.ToLower());
    }
}
