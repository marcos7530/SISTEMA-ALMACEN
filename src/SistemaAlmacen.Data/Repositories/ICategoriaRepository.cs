using SistemaAlmacen.Data.Entities;

namespace SistemaAlmacen.Data.Repositories;

/// <summary>
/// Repositorio específico para la entidad Categoria.
/// </summary>
public interface ICategoriaRepository : IRepository<Categoria>
{
    Task<Categoria?> GetByNameAsync(string nombre);
    Task<bool> HasProductosAsync(int categoriaId);
}
