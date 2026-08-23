using SistemaAlmacen.Business.Interfaces;
using SistemaAlmacen.Data.Repositories;

namespace SistemaAlmacen.Business.Services;

/// <summary>
/// Implementación de ISessionService.
/// La gestión de sesiones es stateless basada en JWT. El bloqueo temporal se persiste en la entidad Usuario.
/// </summary>
public class SessionService : ISessionService
{
    private readonly IUnitOfWork _unitOfWork;

    private const int MaxIntentosFallidos = 5;
    private const int MinutosBloqueo = 15;

    public SessionService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc/>
    public Task<bool> IsSessionActiveAsync(string userId)
    {
        // Sesión stateless basada en JWT: la validez se verifica en el middleware de autenticación.
        // Si el token JWT es válido y no ha expirado, la sesión está activa.
        return Task.FromResult(true);
    }

    /// <inheritdoc/>
    public Task InvalidateSessionAsync(string userId)
    {
        // Con JWT stateless, la invalidación ocurre en el cliente descartando el token.
        // En una implementación futura se podría agregar una lista negra de tokens.
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task RecordLoginAttemptAsync(string email, bool success)
    {
        var usuario = await _unitOfWork.Usuarios.GetByEmailAsync(email);
        if (usuario is null)
            return;

        if (success)
        {
            // Login exitoso: reiniciar contador de intentos fallidos y desbloquear
            usuario.IntentosFallidos = 0;
            usuario.BloqueadoHasta = null;
        }
        else
        {
            // Login fallido: incrementar contador
            usuario.IntentosFallidos++;

            // Si alcanza el máximo de intentos, bloquear por 15 minutos
            if (usuario.IntentosFallidos >= MaxIntentosFallidos)
            {
                usuario.BloqueadoHasta = DateTime.UtcNow.AddMinutes(MinutosBloqueo);
            }
        }

        _unitOfWork.Usuarios.Update(usuario);
        await _unitOfWork.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task<bool> IsAccountLockedAsync(string email)
    {
        var usuario = await _unitOfWork.Usuarios.GetByEmailAsync(email);
        if (usuario is null)
            return false;

        // Si tiene fecha de bloqueo y aún no ha pasado, está bloqueada
        if (usuario.BloqueadoHasta.HasValue && usuario.BloqueadoHasta.Value > DateTime.UtcNow)
            return true;

        // Si tenía bloqueo pero ya expiró, limpiar el bloqueo
        if (usuario.BloqueadoHasta.HasValue && usuario.BloqueadoHasta.Value <= DateTime.UtcNow)
        {
            usuario.BloqueadoHasta = null;
            usuario.IntentosFallidos = 0;
            _unitOfWork.Usuarios.Update(usuario);
            await _unitOfWork.SaveChangesAsync();
        }

        return false;
    }
}
