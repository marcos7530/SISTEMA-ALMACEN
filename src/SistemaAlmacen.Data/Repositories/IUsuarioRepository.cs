using SistemaAlmacen.Data.Entities;
using SistemaAlmacen.Shared.DTOs;

namespace SistemaAlmacen.Data.Repositories;

/// <summary>
/// Repositorio específico para la entidad Usuario.
/// </summary>
public interface IUsuarioRepository : IRepository<Usuario>
{
    Task<Usuario?> GetByEmailAsync(string email);

    /// <summary>
    /// Lista usuarios paginados con búsqueda y filtro de estado opcional.
    /// activo: null = todos, true = solo activos, false = solo inactivos.
    /// </summary>
    Task<PaginatedResult<Usuario>> GetUsuariosAsync(string? searchTerm, bool? activo, int page, int pageSize);
    Task<int> CountActiveAdminsAsync();
}
