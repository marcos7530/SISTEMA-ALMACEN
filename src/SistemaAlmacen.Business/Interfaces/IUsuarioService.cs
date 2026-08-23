using SistemaAlmacen.Shared.Common;
using SistemaAlmacen.Shared.DTOs;
using SistemaAlmacen.Shared.DTOs.Usuarios;

namespace SistemaAlmacen.Business.Interfaces;

/// <summary>
/// Servicio de gestión de usuarios: CRUD completo con eliminación lógica,
/// búsqueda parcial, validaciones de unicidad y reglas de negocio.
/// </summary>
public interface IUsuarioService
{
    /// <summary>
    /// Obtiene usuarios activos con paginación, orden alfabético y búsqueda opcional.
    /// </summary>
    Task<PaginatedResult<UsuarioDto>> GetUsuariosAsync(UsuarioFilter filter);

    /// <summary>
    /// Obtiene un usuario activo por su ID.
    /// </summary>
    Task<UsuarioDto?> GetByIdAsync(int id);

    /// <summary>
    /// Crea un nuevo usuario con contraseña cifrada.
    /// Valida nombre, email (formato y unicidad entre activos) y contraseña.
    /// </summary>
    Task<Result<UsuarioDto>> CreateAsync(CreateUsuarioRequest request);

    /// <summary>
    /// Actualiza nombre, email y rol de un usuario activo existente.
    /// Valida nombre, email (formato y unicidad excluyendo usuario actual).
    /// </summary>
    Task<Result<UsuarioDto>> UpdateAsync(int id, UpdateUsuarioRequest request);

    /// <summary>
    /// Desactiva un usuario (eliminación lógica).
    /// No permite eliminar cuenta propia ni el último administrador activo.
    /// </summary>
    Task<Result> DeactivateAsync(int id, int currentUserId);
}
