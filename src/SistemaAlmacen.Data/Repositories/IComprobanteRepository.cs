using SistemaAlmacen.Data.Entities;

namespace SistemaAlmacen.Data.Repositories;

/// <summary>
/// Repositorio específico para la entidad Comprobante.
/// </summary>
public interface IComprobanteRepository : IRepository<Comprobante>
{
    /// <summary>
    /// Obtiene la factura de una venta (comprobante que no es nota de crédito).
    /// </summary>
    Task<Comprobante?> GetByVentaIdAsync(int ventaId);

    /// <summary>Obtiene todos los comprobantes de una venta (factura y notas de crédito).</summary>
    Task<List<Comprobante>> GetTodosPorVentaIdAsync(int ventaId);

    /// <summary>Obtiene la nota de crédito emitida para una venta, si existe.</summary>
    Task<Comprobante?> GetNotaCreditoPorVentaIdAsync(int ventaId);

    Task<List<Comprobante>> GetPendientesAsync();
}
