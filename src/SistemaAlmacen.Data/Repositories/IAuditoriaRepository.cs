using SistemaAlmacen.Data.Entities;
using SistemaAlmacen.Shared.DTOs;
using SistemaAlmacen.Shared.DTOs.Auditoria;

namespace SistemaAlmacen.Data.Repositories;

/// <summary>
/// Repositorio específico para la entidad AuditoriaLog.
/// </summary>
public interface IAuditoriaRepository : IRepository<AuditoriaLog>
{
    Task<PaginatedResult<AuditoriaLog>> GetHistorialAsync(AuditoriaFilter filter);
}
