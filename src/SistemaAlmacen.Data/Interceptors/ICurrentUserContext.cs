namespace SistemaAlmacen.Data.Interceptors;

/// <summary>
/// Servicio scoped que transporta el Id del usuario actual dentro de un scope de DI.
///
/// Flujo normal (request HTTP): el usuario se obtiene del HttpContext y este servicio
/// no se usa. Flujo en background (Task.Run con scope propio, sin HttpContext): el
/// controller captura el usuarioId dentro del request y lo inyecta en el scope del
/// background task mediante <see cref="SetUserId"/>, de modo que el AuditoriaInterceptor
/// pueda atribuir la auditoría al usuario que originó la operación.
/// </summary>
public interface ICurrentUserContext
{
    /// <summary>
    /// Id del usuario inyectado manualmente en el scope, o null si no se estableció.
    /// </summary>
    int? UserId { get; }

    /// <summary>
    /// Establece el Id del usuario actual para este scope.
    /// </summary>
    void SetUserId(int userId);
}

/// <summary>
/// Implementación scoped de <see cref="ICurrentUserContext"/>. Al ser scoped, cada
/// scope de DI (incluido el creado por <c>IServiceScopeFactory.CreateScope</c> en un
/// background task) tiene su propia instancia aislada.
/// </summary>
public class CurrentUserContext : ICurrentUserContext
{
    public int? UserId { get; private set; }

    public void SetUserId(int userId) => UserId = userId;
}
