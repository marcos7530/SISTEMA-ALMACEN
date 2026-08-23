using SistemaAlmacen.Shared.DTOs.Productos;

namespace SistemaAlmacen.Client.Offline;

/// <summary>
/// Stores and retrieves pending sales and cached product data locally
/// using browser storage (localStorage via JS Interop).
/// </summary>
public interface IOfflineStorageService
{
    /// <summary>
    /// Stores a pending sale that could not be sent to the server.
    /// </summary>
    Task StorePendingVentaAsync(PendingVenta venta);

    /// <summary>
    /// Retrieves all pending sales in chronological order.
    /// </summary>
    Task<List<PendingVenta>> GetPendingVentasAsync();

    /// <summary>
    /// Removes a pending sale by its local ID after successful sync.
    /// </summary>
    Task RemoveVentaAsync(string localId);

    /// <summary>
    /// Gets the count of pending operations awaiting synchronization.
    /// </summary>
    Task<int> GetPendingCountAsync();

    /// <summary>
    /// Caches the product catalog locally for offline use.
    /// </summary>
    Task CacheProductosAsync(List<ProductoDto> productos);

    /// <summary>
    /// Retrieves the cached product catalog.
    /// </summary>
    Task<List<ProductoDto>> GetCachedProductosAsync();

    /// <summary>
    /// Clears all locally stored offline data.
    /// </summary>
    Task ClearAllAsync();
}
