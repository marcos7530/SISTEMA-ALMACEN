using SistemaAlmacen.Business.Interfaces;
using SistemaAlmacen.Data.Entities;
using SistemaAlmacen.Data.Repositories;
using SistemaAlmacen.Shared.DTOs.Auth;

namespace SistemaAlmacen.Business.Services;

/// <summary>
/// Implementación de IAuthService: login, logout y recuperación de contraseña.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly ISessionService _sessionService;

    public AuthService(
        IUnitOfWork unitOfWork,
        ITokenService tokenService,
        ISessionService sessionService)
    {
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _sessionService = sessionService;
    }

    /// <inheritdoc/>
    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        // Verificar si la cuenta está bloqueada
        if (await _sessionService.IsAccountLockedAsync(request.Email))
        {
            return new AuthResult
            {
                Success = false,
                ErrorMessage = "La cuenta se encuentra bloqueada temporalmente. Intente nuevamente en 15 minutos."
            };
        }

        // Buscar usuario por email
        var usuario = await _unitOfWork.Usuarios.GetByEmailAsync(request.Email);

        if (usuario is null)
        {
            // No revelar que el email no existe
            return new AuthResult
            {
                Success = false,
                ErrorMessage = "Las credenciales ingresadas son incorrectas."
            };
        }

        // Verificar que el usuario esté activo
        if (!usuario.Activo)
        {
            return new AuthResult
            {
                Success = false,
                ErrorMessage = "La cuenta se encuentra deshabilitada."
            };
        }

        // Verificar contraseña con BCrypt
        if (!BCrypt.Net.BCrypt.Verify(request.Password, usuario.PasswordHash))
        {
            // Registrar intento fallido
            await _sessionService.RecordLoginAttemptAsync(request.Email, success: false);

            return new AuthResult
            {
                Success = false,
                ErrorMessage = "Las credenciales ingresadas son incorrectas."
            };
        }

        // Login exitoso: registrar intento exitoso (reinicia contador)
        await _sessionService.RecordLoginAttemptAsync(request.Email, success: true);

        // Generar token JWT
        var token = _tokenService.GenerateJwtToken(usuario);

        return new AuthResult
        {
            Success = true,
            Token = token,
            Rol = usuario.Rol.ToString()
        };
    }

    /// <inheritdoc/>
    public Task LogoutAsync()
    {
        // Con JWT stateless, el logout se maneja en el cliente descartando el token.
        // El servidor no necesita hacer nada adicional en este modelo.
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<bool> RequestPasswordRecoveryAsync(string email)
    {
        // Siempre retornar true para no revelar si el email existe (Req 5.2)
        var usuario = await _unitOfWork.Usuarios.GetByEmailAsync(email);
        if (usuario is null || !usuario.Activo)
            return true;

        // Generar token de recuperación
        var token = _tokenService.GenerateRecoveryToken();

        var tokenRecuperacion = new TokenRecuperacion
        {
            UsuarioId = usuario.Id,
            Token = token,
            Usado = false,
            FechaCreacion = DateTime.UtcNow,
            FechaExpiracion = DateTime.UtcNow.AddHours(24)
        };

        // Persistir token a través de la colección de navegación del usuario
        usuario.TokensRecuperacion.Add(tokenRecuperacion);
        _unitOfWork.Usuarios.Update(usuario);
        await _unitOfWork.SaveChangesAsync();

        // TODO (Task 6.2): Enviar email con enlace de recuperación
        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> ResetPasswordAsync(string token, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(newPassword))
            return false;

        // Buscar token válido (no usado, no expirado)
        var usuario = await FindUsuarioByValidTokenAsync(token);
        if (usuario is null)
            return false;

        var tokenEntity = usuario.TokensRecuperacion
            .FirstOrDefault(t => t.Token == token && !t.Usado && t.FechaExpiracion > DateTime.UtcNow);

        if (tokenEntity is null)
            return false;

        // Actualizar contraseña con BCrypt
        usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

        // Invalidar el token (marcarlo como usado)
        tokenEntity.Usado = true;

        // Reiniciar intentos fallidos y desbloquear
        usuario.IntentosFallidos = 0;
        usuario.BloqueadoHasta = null;

        _unitOfWork.Usuarios.Update(usuario);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> ValidateRecoveryTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        var usuario = await FindUsuarioByValidTokenAsync(token);
        return usuario is not null;
    }

    /// <summary>
    /// Busca un usuario que tenga un token de recuperación válido (no usado, no expirado).
    /// Nota: En task 6.2 se implementará un repositorio dedicado para TokenRecuperacion
    /// para evitar cargar todos los usuarios.
    /// </summary>
    private async Task<Usuario?> FindUsuarioByValidTokenAsync(string token)
    {
        // Buscar en todos los usuarios activos (se optimizará con repositorio dedicado en task 6.2)
        var usuarios = await _unitOfWork.Usuarios.GetAllAsync();

        return usuarios.FirstOrDefault(u =>
            u.Activo &&
            u.TokensRecuperacion.Any(t =>
                t.Token == token &&
                !t.Usado &&
                t.FechaExpiracion > DateTime.UtcNow));
    }
}
