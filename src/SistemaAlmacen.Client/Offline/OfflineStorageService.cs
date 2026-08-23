using System.Text.Json;
using Microsoft.JSInterop;
using SistemaAlmacen.Shared.DTOs.Productos;

namespace SistemaAlmacen.Client.Offline;

/// <summary>
/// Stores and retrieves offline data using browser localStorage via JS Interop.
/// Uses localStorage for simplicity and reliability with Blazor WASM.
/// Data persists across browser sessions.
/// </summary>
public class OfflineStorageService : IOfflineStorageService
{
    private readonly IJSRuntime _jsRuntime;

    private const string PendingVentasKey = "offline_pending_ventas";
    private const string CachedProductosKey = "offline_cached_productos";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public OfflineStorageService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task StorePendingVentaAsync(PendingVenta venta)
    {
        var ventas = await GetPendingVentasAsync();
        ventas.Add(venta);
        var json = JsonSerializer.Serialize(ventas, JsonOptions);
        await _jsRuntime.InvokeVoidAsync("offlineInterop.setItem", PendingVentasKey, json);
    }

    public async Task<List<PendingVenta>> GetPendingVentasAsync()
    {
        var json = await _jsRuntime.InvokeAsync<string?>("offlineInterop.getItem", PendingVentasKey);
        if (string.IsNullOrWhiteSpace(json))
            return new List<PendingVenta>();

        return JsonSerializer.Deserialize<List<PendingVenta>>(json, JsonOptions) ?? new List<PendingVenta>();
    }

    public async Task RemoveVentaAsync(string localId)
    {
        var ventas = await GetPendingVentasAsync();
        ventas.RemoveAll(v => v.LocalId == localId);
        var json = JsonSerializer.Serialize(ventas, JsonOptions);
        await _jsRuntime.InvokeVoidAsync("offlineInterop.setItem", PendingVentasKey, json);
    }

    public async Task<int> GetPendingCountAsync()
    {
        var ventas = await GetPendingVentasAsync();
        return ventas.Count;
    }

    public async Task CacheProductosAsync(List<ProductoDto> productos)
    {
        var json = JsonSerializer.Serialize(productos, JsonOptions);
        await _jsRuntime.InvokeVoidAsync("offlineInterop.setItem", CachedProductosKey, json);
    }

    public async Task<List<ProductoDto>> GetCachedProductosAsync()
    {
        var json = await _jsRuntime.InvokeAsync<string?>("offlineInterop.getItem", CachedProductosKey);
        if (string.IsNullOrWhiteSpace(json))
            return new List<ProductoDto>();

        return JsonSerializer.Deserialize<List<ProductoDto>>(json, JsonOptions) ?? new List<ProductoDto>();
    }

    public async Task ClearAllAsync()
    {
        await _jsRuntime.InvokeVoidAsync("offlineInterop.removeItem", PendingVentasKey);
        await _jsRuntime.InvokeVoidAsync("offlineInterop.removeItem", CachedProductosKey);
    }
}
