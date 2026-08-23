using SistemaAlmacen.Data.Entities;

namespace SistemaAlmacen.Data.Repositories;

/// <summary>
/// Repositorio específico para la entidad TokenRecuperacion.
/// </summary>
public interface ITokenRecuperacionRepository : IRepository<TokenRecuperacion>
{
    /// <summary>
    /// Obtiene un token de recuperación por su valor de token.
    /// </summary>
    Task<TokenRecuperacion?> GetByTokenAsync(string token);

    /// <summary>
    /// Invalida (marca como usado) todos los tokens activos de un usuario.
    /// </summary>
    Task InvalidateTokensByUsuarioIdAsync(int usuarioId);
}
