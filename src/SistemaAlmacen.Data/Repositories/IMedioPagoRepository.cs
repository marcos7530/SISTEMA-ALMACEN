using SistemaAlmacen.Data.Entities;

namespace SistemaAlmacen.Data.Repositories;

/// <summary>
/// Repositorio específico para la entidad MedioPago.
/// </summary>
public interface IMedioPagoRepository : IRepository<MedioPago>
{
    Task<List<MedioPago>> GetActivosAsync();
    Task<MedioPago?> GetByNameAsync(string nombre);
}
