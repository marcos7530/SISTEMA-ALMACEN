using System.Security.Cryptography;
using System.Text.RegularExpressions;
using SistemaAlmacen.Business.Interfaces;
using SistemaAlmacen.Data.Entities;
using SistemaAlmacen.Data.Repositories;
using SistemaAlmacen.Shared.Common;

namespace SistemaAlmacen.Business.Services;

/// <summary>
/// Implementación del servicio de recuperación de contraseña.
/// Genera tokens seguros, valida expiración y uso único, y restablece contraseñas con requisitos de complejidad.
/// </summary>
public class PasswordRecoveryService : IPasswordRecoveryService
{
    private readonly IUnitOfWork _unitOfWork;

    private const int TokenLength = 32;
    private const int TokenExpirationHours = 24;
    private const int MinPasswordLength = 8;
    private const int MaxPasswordLength = 50;

    // Regex para validar complejidad de contraseña: al menos 1 mayúscula, 1 minúscula, 1 dígito
    private static readonly Regex UppercaseRegex = new(@"[A-Z]", RegexOptions.Compiled);
    private static readonly Regex LowercaseRegex = new(@"[a-z]", RegexOptions.Compiled);
    private static readonly Regex DigitRegex = new(@"\d", RegexOptions.Compiled);

    public PasswordRecoveryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<bool> RequestRecoveryAsync(string email)
    {
        // Buscar usuario activo por email
        var usuario = await _unitOfWork.Usuarios.GetByEmailAsync(email);

        // Si no existe o no está activo, retornar true de todas formas (Req 5.2: respuesta genérica)
        if (usuario == null || !usuario.Activo)
        {
            return true;
        }

        // Invalidar tokens previos no usados del usuario
        await _unitOfWork.TokensRecuperacion.InvalidateTokensByUsuarioIdAsync(usuario.Id);

        // Generar nuevo token seguro
        var tokenValue = GenerateSecureToken();

        var tokenEntity = new TokenRecuperacion
        {
            UsuarioId = usuario.Id,
            Token = tokenValue,
            Usado = false,
            FechaCreacion = DateTime.UtcNow,
            FechaExpiracion = DateTime.UtcNow.AddHours(TokenExpirationHours)
        };

        await _unitOfWork.TokensRecuperacion.AddAsync(tokenEntity);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> ValidateTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var tokenEntity = await _unitOfWork.TokensRecuperacion.GetByTokenAsync(token);

        return IsTokenValid(tokenEntity);
    }

    /// <inheritdoc />
    public async Task<Result> ResetPasswordAsync(string token, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Result.Failure("El enlace de recuperación no es válido.", "TOKEN_INVALIDO");
        }

        // Validar token
        var tokenEntity = await _unitOfWork.TokensRecuperacion.GetByTokenAsync(token);

        if (!IsTokenValid(tokenEntity))
        {
            return Result.Failure("El enlace ya no es válido.", "TOKEN_INVALIDO");
        }

        // Validar complejidad de nueva contraseña
        var passwordValidation = ValidatePasswordComplexity(newPassword);
        if (!passwordValidation.IsSuccess)
        {
            return passwordValidation;
        }

        // Hash de nueva contraseña con BCrypt
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

        // Actualizar contraseña del usuario
        var usuario = tokenEntity!.Usuario;
        usuario.PasswordHash = passwordHash;
        usuario.IntentosFallidos = 0;
        usuario.BloqueadoHasta = null;
        _unitOfWork.Usuarios.Update(usuario);

        // Marcar token como usado
        tokenEntity.Usado = true;
        _unitOfWork.TokensRecuperacion.Update(tokenEntity);

        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    /// <summary>
    /// Genera un token alfanumérico seguro de al menos 32 caracteres usando RandomNumberGenerator.
    /// </summary>
    private static string GenerateSecureToken()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var tokenChars = new char[TokenLength];
        var randomBytes = RandomNumberGenerator.GetBytes(TokenLength);

        for (int i = 0; i < TokenLength; i++)
        {
            tokenChars[i] = chars[randomBytes[i] % chars.Length];
        }

        return new string(tokenChars);
    }

    /// <summary>
    /// Verifica si un token es válido: existe, no usado, no expirado.
    /// </summary>
    private static bool IsTokenValid(TokenRecuperacion? tokenEntity)
    {
        if (tokenEntity == null)
        {
            return false;
        }

        if (tokenEntity.Usado)
        {
            return false;
        }

        if (tokenEntity.FechaExpiracion <= DateTime.UtcNow)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Valida que la contraseña cumple los requisitos de complejidad:
    /// mínimo 8 caracteres, máximo 50, al menos una mayúscula, una minúscula y un número.
    /// </summary>
    private static Result ValidatePasswordComplexity(string password)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrEmpty(password))
        {
            errors["Password"] = ["La contraseña es requerida."];
            return Result.ValidationFailure(errors);
        }

        var passwordErrors = new List<string>();

        if (password.Length < MinPasswordLength)
        {
            passwordErrors.Add($"La contraseña debe tener al menos {MinPasswordLength} caracteres.");
        }

        if (password.Length > MaxPasswordLength)
        {
            passwordErrors.Add($"La contraseña no puede exceder {MaxPasswordLength} caracteres.");
        }

        if (!UppercaseRegex.IsMatch(password))
        {
            passwordErrors.Add("La contraseña debe contener al menos una letra mayúscula.");
        }

        if (!LowercaseRegex.IsMatch(password))
        {
            passwordErrors.Add("La contraseña debe contener al menos una letra minúscula.");
        }

        if (!DigitRegex.IsMatch(password))
        {
            passwordErrors.Add("La contraseña debe contener al menos un número.");
        }

        if (passwordErrors.Count > 0)
        {
            errors["Password"] = passwordErrors.ToArray();
            return Result.ValidationFailure(errors);
        }

        return Result.Success();
    }
}
