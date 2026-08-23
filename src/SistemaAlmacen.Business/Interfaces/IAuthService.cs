using SistemaAlmacen.Shared.DTOs.Auth;

namespace SistemaAlmacen.Business.Interfaces;

/// <summary>
/// Servicio de autenticación: login, logout y recuperación de contraseña.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Autentica al usuario con email y contraseña. Retorna token JWT si las credenciales son válidas.
    /// </summary>
    Task<AuthResult> LoginAsync(LoginRequest request);

    /// <summary>
    /// Cierra la sesión del usuario actual (invalida token del lado del cliente).
    /// </summary>
    Task LogoutAsync();

    /// <summary>
    /// Solicita recuperación de contraseña por email. Siempre retorna true (no revela si el email existe).
    /// </summary>
    Task<bool> RequestPasswordRecoveryAsync(string email);

    /// <summary>
    /// Restablece la contraseña usando un token de recuperación válido.
    /// </summary>
    Task<bool> ResetPasswordAsync(string token, string newPassword);

    /// <summary>
    /// Valida si un token de recuperación es válido (no expirado, no usado).
    /// </summary>
    Task<bool> ValidateRecoveryTokenAsync(string token);
}
