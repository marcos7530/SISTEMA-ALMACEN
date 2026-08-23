using System.Security.Claims;
using SistemaAlmacen.Data.Entities;

namespace SistemaAlmacen.Business.Interfaces;

/// <summary>
/// Servicio para generación y validación de tokens JWT y de recuperación.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Genera un token JWT para el usuario autenticado con claims de UserId, Email, Rol y Name.
    /// </summary>
    string GenerateJwtToken(Usuario usuario);

    /// <summary>
    /// Genera un token alfanumérico de recuperación de contraseña (mínimo 32 caracteres).
    /// </summary>
    string GenerateRecoveryToken();

    /// <summary>
    /// Valida un token JWT y retorna el ClaimsPrincipal. Retorna null si el token es inválido o expirado.
    /// </summary>
    ClaimsPrincipal? ValidateToken(string token);
}
