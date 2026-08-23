namespace SistemaAlmacen.Client.Offline;

/// <summary>
/// Estados posibles del motor de sincronización.
/// </summary>
public enum SyncStatus
{
    /// <summary>Motor inactivo, sin operaciones pendientes.</summary>
    Idle,

    /// <summary>Sincronización en progreso.</summary>
    Syncing,

    /// <summary>Sincronización completada exitosamente.</summary>
    Completed,

    /// <summary>Error durante la sincronización.</summary>
    Error,

    /// <summary>Sin conexión al servidor.</summary>
    Offline
}

/// <summary>
/// Evento que notifica cambios en el estado del motor de sincronización.
/// </summary>
public class SyncStatusChangedEventArgs : EventArgs
{
    /// <summary>Estado actual del motor.</summary>
    public SyncStatus Status { get; }

    /// <summary>Mensaje descriptivo del estado actual.</summary>
    public string? Message { get; }

    /// <summary>Cantidad de operaciones pendientes.</summary>
    public int PendingCount { get; }

    public SyncStatusChangedEventArgs(SyncStatus status, int pendingCount, string? message = null)
    {
        Status = status;
        PendingCount = pendingCount;
        Message = message;
    }
}

/// <summary>
/// Motor de sincronización que gestiona la transferencia de operaciones offline al servidor.
/// Se activa automáticamente cuando se recupera la conexión.
/// </summary>
public interface ISyncEngine
{
    /// <summary>
    /// Obtiene el estado actual del motor de sincronización.
    /// </summary>
    Task<SyncStatus> GetStatusAsync();

    /// <summary>
    /// Sincroniza todas las operaciones pendientes en orden cronológico.
    /// Detecta conflictos (stock insuficiente) y notifica al administrador.
    /// Elimina la copia local tras sincronización exitosa.
    /// </summary>
    Task SyncPendingOperationsAsync();

    /// <summary>
    /// Retorna la cantidad de operaciones pendientes de sincronización.
    /// </summary>
    Task<int> GetPendingCountAsync();

    /// <summary>
    /// Evento disparado cuando cambia el estado del motor de sincronización.
    /// </summary>
    event EventHandler<SyncStatusChangedEventArgs> StatusChanged;
}
