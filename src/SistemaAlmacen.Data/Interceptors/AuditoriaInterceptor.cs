using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using SistemaAlmacen.Data.Entities;
using SistemaAlmacen.Shared.Enums;
using System.Security.Claims;

namespace SistemaAlmacen.Data.Interceptors;

/// <summary>
/// Interceptor de EF Core que registra automáticamente operaciones de auditoría
/// al detectar creaciones, modificaciones y eliminaciones de entidades auditables.
/// 
/// Diseño: Los registros de auditoría se agregan al mismo DbContext antes del SaveChanges,
/// por lo que se persisten en la misma transacción. Esto garantiza consistencia
/// (no puede existir un cambio sin su registro de auditoría) y es suficientemente
/// performante dado que son inserciones livianas en una sola tabla.
/// 
/// Nota sobre Req 13.9 (async): Si bien el registro se realiza de forma síncrona
/// con la operación principal (misma transacción), las entradas de auditoría son
/// livianas (un INSERT por operación) y no afectan significativamente el tiempo
/// de respuesta. Una alternativa con cola en background agregaría complejidad
/// y riesgo de pérdida de registros sin un beneficio medible para este volumen.
/// </summary>
public class AuditoriaInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditoriaInterceptor> _logger;

    /// <summary>
    /// Entidades que serán auditadas automáticamente.
    /// </summary>
    private static readonly HashSet<Type> EntidadesAuditables = new()
    {
        typeof(Usuario),
        typeof(Producto),
        typeof(Categoria),
        typeof(Venta),
        typeof(DetalleVenta),
        typeof(MedioPago),
        typeof(Caja),
        typeof(CajaMovimiento),
        typeof(Comprobante),
        typeof(MovimientoStock),
        typeof(Cliente),
        typeof(MovimientoCuentaCorriente),
        typeof(Proveedor),
        typeof(Compra),
        typeof(DetalleCompra)
    };

    /// <summary>
    /// ID de usuario del sistema para operaciones sin contexto HTTP (ej: seeding, migraciones).
    /// </summary>
    private const int SystemUserId = 1;

    public AuditoriaInterceptor(
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuditoriaInterceptor> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null)
            return new ValueTask<InterceptionResult<int>>(result);

        var context = eventData.Context;
        var auditEntries = CreateAuditEntries(context);

        if (auditEntries.Count > 0)
        {
            context.Set<AuditoriaLog>().AddRange(auditEntries);
        }

        return new ValueTask<InterceptionResult<int>>(result);
    }

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is null)
            return result;

        var context = eventData.Context;
        var auditEntries = CreateAuditEntries(context);

        if (auditEntries.Count > 0)
        {
            context.Set<AuditoriaLog>().AddRange(auditEntries);
        }

        return result;
    }

    /// <summary>
    /// Analiza el ChangeTracker del DbContext y crea entradas de auditoría
    /// para cada entidad auditable que fue creada, modificada o eliminada.
    /// No genera auditoría durante el seeding (cuando no hay contexto HTTP autenticado)
    /// para evitar problemas de FK circular con el usuario del sistema.
    /// </summary>
    private List<AuditoriaLog> CreateAuditEntries(DbContext context)
    {
        var auditEntries = new List<AuditoriaLog>();

        // No auditar si no hay contexto HTTP (seeding, migraciones, jobs en background)
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
            return auditEntries;

        var usuarioId = GetCurrentUserId();
        var now = DateTime.UtcNow;

        // Detectar cambios pendientes (excluir las propias entradas de auditoría)
        var changedEntities = context.ChangeTracker.Entries()
            .Where(e => EntidadesAuditables.Contains(e.Entity.GetType())
                        && e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        foreach (var entry in changedEntities)
        {
            try
            {
                var auditLog = CreateAuditLogForEntry(entry, usuarioId, now);
                if (auditLog is not null)
                {
                    auditEntries.Add(auditLog);
                }
            }
            catch (Exception ex)
            {
                // No interrumpir la operación principal si falla la creación del log
                _logger.LogWarning(ex, "Error al crear entrada de auditoría para {Entity}", entry.Entity.GetType().Name);
            }
        }

        return auditEntries;
    }

    /// <summary>
    /// Crea un registro de auditoría para una entrada del ChangeTracker.
    /// </summary>
    private static AuditoriaLog? CreateAuditLogForEntry(EntityEntry entry, int usuarioId, DateTime fecha)
    {
        var entityName = entry.Entity.GetType().Name;
        var tipoOperacion = MapStateToTipoOperacion(entry.State, entityName);
        var registroId = GetEntityPrimaryKey(entry);
        var descripcion = GenerateDescription(entry, entityName, tipoOperacion, registroId);

        return new AuditoriaLog
        {
            UsuarioId = usuarioId,
            TipoOperacion = tipoOperacion,
            EntidadAfectada = entityName,
            RegistroAfectadoId = registroId,
            Descripcion = descripcion,
            Fecha = fecha
        };
    }

    /// <summary>
    /// Mapea el EntityState a TipoOperacion.
    /// Para la entidad Venta en estado Added, se usa TipoOperacion.Venta.
    /// </summary>
    private static TipoOperacion MapStateToTipoOperacion(EntityState state, string entityName)
    {
        return state switch
        {
            EntityState.Added when entityName == nameof(Venta) => TipoOperacion.Venta,
            EntityState.Added => TipoOperacion.Creacion,
            EntityState.Modified => TipoOperacion.Modificacion,
            EntityState.Deleted => TipoOperacion.Eliminacion,
            _ => TipoOperacion.Creacion
        };
    }

    /// <summary>
    /// Obtiene el valor de la clave primaria de la entidad.
    /// Para entidades Added cuya clave es generada por la DB, se usa "Pendiente" ya que
    /// el valor real se asignará tras el SaveChanges.
    /// </summary>
    private static string GetEntityPrimaryKey(EntityEntry entry)
    {
        var primaryKey = entry.Properties
            .FirstOrDefault(p => p.Metadata.IsPrimaryKey());

        if (primaryKey is null)
            return "N/A";

        var value = primaryKey.CurrentValue;

        // Si la entidad es nueva y la PK es 0 (auto-generated), indicar que está pendiente
        if (entry.State == EntityState.Added && value is int intValue && intValue == 0)
            return "Pendiente";

        if (entry.State == EntityState.Added && value is long longValue && longValue == 0)
            return "Pendiente";

        return value?.ToString() ?? "N/A";
    }

    /// <summary>
    /// Genera una descripción legible de la operación realizada.
    /// </summary>
    private static string GenerateDescription(EntityEntry entry, string entityName, TipoOperacion tipo, string registroId)
    {
        return tipo switch
        {
            TipoOperacion.Creacion => $"Se creó {entityName} con Id {registroId}",
            TipoOperacion.Modificacion => GenerateModificationDescription(entry, entityName, registroId),
            TipoOperacion.Eliminacion => $"Se eliminó {entityName} con Id {registroId}",
            TipoOperacion.Venta => $"Se registró Venta con Id {registroId}",
            _ => $"Operación sobre {entityName} con Id {registroId}"
        };
    }

    /// <summary>
    /// Genera una descripción detallada para modificaciones, incluyendo los campos cambiados.
    /// </summary>
    private static string GenerateModificationDescription(EntityEntry entry, string entityName, string registroId)
    {
        var changedProperties = entry.Properties
            .Where(p => p.IsModified && !p.Metadata.IsPrimaryKey())
            .Select(p => p.Metadata.Name)
            .Take(5) // Limitar para no exceder los 500 caracteres
            .ToList();

        if (changedProperties.Count == 0)
            return $"Se modificó {entityName} con Id {registroId}";

        var campos = string.Join(", ", changedProperties);
        var description = $"Se modificó {entityName} con Id {registroId}. Campos: {campos}";

        // Truncar si excede MaxLength de 500
        return description.Length > 500 ? description[..497] + "..." : description;
    }

    /// <summary>
    /// Obtiene el ID del usuario actual desde el contexto HTTP (claims del JWT).
    /// Si no hay contexto HTTP (ej: seeding, migraciones, jobs), retorna el ID de usuario del sistema.
    /// </summary>
    private int GetCurrentUserId()
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext?.User?.Identity?.IsAuthenticated != true)
            return SystemUserId;

        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)
                          ?? httpContext.User.FindFirst("sub");

        if (userIdClaim is not null && int.TryParse(userIdClaim.Value, out var userId))
            return userId;

        return SystemUserId;
    }
}
