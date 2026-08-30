using SistemaAlmacen.Data.Entities;

namespace SistemaAlmacen.Data.Repositories;

/// <summary>
/// Repositorio específico para la entidad MovimientoStock.
/// </summary>
public interface IMovimientoStockRepository : IRepository<MovimientoStock>
{
    /// <summary>
    /// Obtiene el historial de movimientos de un producto, ordenado del más reciente al más antiguo.
    /// </summary>
    Task<List<MovimientoStock>> GetByProductoAsync(int productoId);
}
