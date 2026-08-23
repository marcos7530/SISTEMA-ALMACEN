namespace SistemaAlmacen.Business.Interfaces;

/// <summary>
/// Servicio de gestión de sesiones: registro de intentos, bloqueo y actividad.
/// </summary>
public interface ISessionService
{
    /// <summary>
    /// Verifica si la sesión del usuario está activa (basado en expiración de JWT - stateless).
    /// </summary>
    Task<bool> IsSessionActiveAsync(string userId);

    /// <summary>
    /// Invalida la sesión del usuario (para logout, el cliente descarta el token).
    /// </summary>
    Task InvalidateSessionAsync(string userId);

    /// <summary>
    /// Registra un intento de login (exitoso o fallido). 
    /// Si es fallido, incrementa IntentosFallidos. Si alcanza 5, bloquea por 15 minutos.
    /// Si es exitoso, reinicia IntentosFallidos a 0.
    /// </summary>
    Task RecordLoginAttemptAsync(string email, bool success);

    /// <summary>
    /// Verifica si la cuenta está bloqueada temporalmente por intentos fallidos.
    /// </summary>
    Task<bool> IsAccountLockedAsync(string email);
}
