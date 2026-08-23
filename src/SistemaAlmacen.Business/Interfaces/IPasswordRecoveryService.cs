using SistemaAlmacen.Shared.Common;

namespace SistemaAlmacen.Business.Interfaces;

/// <summary>
/// Servicio de recuperación de contraseña.
/// Gestiona la generación de tokens, validación y restablecimiento de contraseña.
/// </summary>
public interface IPasswordRecoveryService
{
    /// <summary>
    /// Solicita recuperación de contraseña para un email.
    /// Siempre retorna true independientemente de si el email existe (Req 5.2).
    /// </summary>
    /// <param name="email">Email del usuario que solicita la recuperación.</param>
    /// <returns>Siempre true (respuesta genérica).</returns>
    Task<bool> RequestRecoveryAsync(string email);

    /// <summary>
    /// Valida si un token de recuperación es válido (existe, no usado, no expirado).
    /// </summary>
    /// <param name="token">Token de recuperación a validar.</param>
    /// <returns>True si el token es válido, false en caso contrario.</returns>
    Task<bool> ValidateTokenAsync(string token);

    /// <summary>
    /// Restablece la contraseña del usuario asociado al token.
    /// Valida complejidad de contraseña, marca token como usado y resetea intentos fallidos.
    /// </summary>
    /// <param name="token">Token de recuperación válido.</param>
    /// <param name="newPassword">Nueva contraseña que cumple requisitos de complejidad.</param>
    /// <returns>Result indicando éxito o error con detalles.</returns>
    Task<Result> ResetPasswordAsync(string token, string newPassword);
}
