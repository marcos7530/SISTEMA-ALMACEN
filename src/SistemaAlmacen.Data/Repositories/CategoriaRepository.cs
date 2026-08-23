using Microsoft.EntityFrameworkCore;
using SistemaAlmacen.Data.Context;
using SistemaAlmacen.Data.Entities;

namespace SistemaAlmacen.Data.Repositories;

/// <summary>
/// Implementación del repositorio de categorías.
/// </summary>
public class CategoriaRepository : Repository<Categoria>, ICategoriaRepository
{
    public CategoriaRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Categoria?> GetByNameAsync(string nombre)
    {
        return await _dbSet
            .FirstOrDefaultAsync(c => c.Nombre.ToLower() == nombre.ToLower());
    }

    public async Task<bool> HasProductosAsync(int categoriaId)
    {
        return await _context.Productos
            .AnyAsync(p => p.CategoriaId == categoriaId && p.Activo);
    }
}
