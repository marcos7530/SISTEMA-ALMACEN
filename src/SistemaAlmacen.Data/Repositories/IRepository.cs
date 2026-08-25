using System.Linq.Expressions;
using SistemaAlmacen.Shared.DTOs;

namespace SistemaAlmacen.Data.Repositories;

/// <summary>
/// Interfaz genérica de repositorio con operaciones CRUD básicas, paginación y filtrado.
/// </summary>
/// <typeparam name="T">Tipo de entidad.</typeparam>
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<List<T>> GetAllAsync();
    Task<PaginatedResult<T>> GetPaginatedAsync(
        int page,
        int pageSize,
        Expression<Func<T, bool>>? filter = null,
        Expression<Func<T, object>>? orderBy = null,
        bool descending = false);
    Task AddAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
    Task<int> CountAsync(Expression<Func<T, bool>>? filter = null);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
}
