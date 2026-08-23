using SistemaAlmacen.Data.Entities;

namespace SistemaAlmacen.Data.Repositories;

/// <summary>
/// Repositorio específico para la entidad Comprobante.
/// </summary>
public interface IComprobanteRepository : IRepository<Comprobante>
{
    Task<Comprobante?> GetByVentaIdAsync(int ventaId);
    Task<List<Comprobante>> GetPendientesAsync();
}
