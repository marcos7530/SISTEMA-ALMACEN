using SistemaAlmacen.Data.Entities;
using SistemaAlmacen.Shared.DTOs;
using SistemaAlmacen.Shared.DTOs.Clientes;

namespace SistemaAlmacen.Data.Repositories;

/// <summary>
/// Repositorio específico para la entidad Cliente y su cuenta corriente.
/// </summary>
public interface IClienteRepository : IRepository<Cliente>
{
    Task<PaginatedResult<Cliente>> GetActivosAsync(ClienteFilter filter);
    Task<Cliente?> GetByDocumentoAsync(string documento);
    Task<bool> ExisteDocumentoActivoAsync(string documento, int? excluirClienteId = null);

    /// <summary>Obtiene los movimientos de cuenta corriente de un cliente, más recientes primero.</summary>
    Task<List<MovimientoCuentaCorriente>> GetMovimientosAsync(int clienteId);

    /// <summary>Calcula el saldo deudor actual del cliente (cargos - pagos - ajustes).</summary>
    Task<decimal> GetSaldoAsync(int clienteId);

    Task AddMovimientoAsync(MovimientoCuentaCorriente movimiento);
}
