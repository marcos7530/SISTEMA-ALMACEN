using SistemaAlmacen.Data.Entities;
using SistemaAlmacen.Shared.DTOs;

namespace SistemaAlmacen.Data.Repositories;

/// <summary>
/// Repositorio específico para la entidad Usuario.
/// </summary>
public interface IUsuarioRepository : IRepository<Usuario>
{
    Task<Usuario?> GetByEmailAsync(string email);
    Task<PaginatedResult<Usuario>> GetActiveUsuariosAsync(string? searchTerm, int page, int pageSize);
    Task<int> CountActiveAdminsAsync();
}
