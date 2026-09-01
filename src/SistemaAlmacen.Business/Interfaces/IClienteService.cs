using SistemaAlmacen.Shared.Common;
using SistemaAlmacen.Shared.DTOs;
using SistemaAlmacen.Shared.DTOs.Clientes;

namespace SistemaAlmacen.Business.Interfaces;

/// <summary>
/// Servicio de lógica de negocio para la gestión de clientes.
/// </summary>
public interface IClienteService
{
    /// <summary>
    /// Obtiene un listado paginado de clientes activos con filtros opcionales.
    /// </summary>
    Task<PaginatedResult<ClienteDto>> GetClientesAsync(ClienteFilter filter);

    /// <summary>
    /// Obtiene un cliente activo por su ID, incluyendo su saldo de cuenta corriente.
    /// Retorna null si no existe o está inactivo.
    /// </summary>
    Task<ClienteDto?> GetByIdAsync(int id);

    /// <summary>
    /// Crea un nuevo cliente con las validaciones de negocio correspondientes.
    /// </summary>
    Task<Result<ClienteDto>> CreateAsync(CreateClienteRequest request);

    /// <summary>
    /// Actualiza un cliente existente con las validaciones de negocio correspondientes.
    /// </summary>
    Task<Result<ClienteDto>> UpdateAsync(int id, UpdateClienteRequest request);

    /// <summary>
    /// Realiza una eliminación lógica de un cliente (marca como inactivo).
    /// </summary>
    Task<Result> DeactivateAsync(int id);
}
