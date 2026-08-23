using System.Net.Http.Json;
using SistemaAlmacen.Shared.DTOs.Ventas;

namespace SistemaAlmacen.Client.Offline;

/// <summary>
/// Resultado de la sincronización de una operación individual.
/// </summary>
public class SyncOperationResult
{
    public string LocalId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public bool HasConflict { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Implementación del motor de sincronización offline.
/// Se suscribe a IConnectivityService.ConnectivityChanged para auto-disparar
/// la sincronización cuando se recupera la conexión.
/// </summary>
public class SyncEngine : ISyncEngine, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly IOfflineStorageService _offlineStorage;
    private readonly IConnectivityService _connectivityService;
    private SyncStatus _currentStatus = SyncStatus.Idle;
    private bool _disposed;

    public event EventHandler<SyncStatusChangedEventArgs>? StatusChanged;

    public SyncEngine(
        HttpClient httpClient,
        IOfflineStorageService offlineStorage,
        IConnectivityService connectivityService)
    {
        _httpClient = httpClient;
        _offlineStorage = offlineStorage;
        _connectivityService = connectivityService;

        // Subscribe to connectivity changes to auto-trigger sync
        _connectivityService.ConnectivityChanged += OnConnectivityChanged;

        // Set initial status based on connectivity
        _currentStatus = _connectivityService.IsOnline ? SyncStatus.Idle : SyncStatus.Offline;
    }

    /// <inheritdoc />
    public Task<SyncStatus> GetStatusAsync()
    {
        return Task.FromResult(_currentStatus);
    }

    /// <inheritdoc />
    public async Task<int> GetPendingCountAsync()
    {
        return await _offlineStorage.GetPendingCountAsync();
    }

    /// <inheritdoc />
    public async Task SyncPendingOperationsAsync()
    {
        if (!_connectivityService.IsOnline)
        {
            SetStatus(SyncStatus.Offline, "Sin conexión al servidor.");
            return;
        }

        var pendingVentas = await _offlineStorage.GetPendingVentasAsync();
        if (pendingVentas.Count == 0)
        {
            SetStatus(SyncStatus.Idle, "No hay operaciones pendientes.");
            return;
        }

        SetStatus(SyncStatus.Syncing, $"Sincronizando {pendingVentas.Count} operación(es)...");

        // Sync in chronological order
        var ordered = pendingVentas.OrderBy(v => v.FechaRegistro).ToList();
        var hasErrors = false;

        foreach (var venta in ordered)
        {
            var result = await SyncSingleVentaAsync(venta);

            if (result.Success)
            {
                // Remove local copy after successful sync
                await _offlineStorage.RemoveVentaAsync(venta.LocalId);
            }
            else if (result.HasConflict)
            {
                // Conflict detected (e.g., stock insuficiente) - notify admin
                hasErrors = true;
                await NotifyConflictAsync(venta, result.ErrorMessage ?? "Conflicto detectado.");
            }
            else
            {
                hasErrors = true;
            }

            // Update pending count after each operation
            var remainingCount = await GetPendingCountAsync();
            RaiseStatusChanged(_currentStatus, remainingCount,
                result.Success ? null : result.ErrorMessage);
        }

        var finalCount = await GetPendingCountAsync();
        if (finalCount == 0)
        {
            SetStatus(SyncStatus.Completed, "Sincronización completada.");
        }
        else if (hasErrors)
        {
            SetStatus(SyncStatus.Error,
                $"Sincronización parcial: {finalCount} operación(es) pendiente(s) con conflictos.");
        }
        else
        {
            SetStatus(SyncStatus.Idle);
        }
    }

    /// <summary>
    /// Sincroniza una venta individual con el servidor.
    /// </summary>
    private async Task<SyncOperationResult> SyncSingleVentaAsync(PendingVenta venta)
    {
        try
        {
            // Step 1: Create the venta on the server
            var initResponse = await _httpClient.PostAsJsonAsync("api/ventas", new { });
            if (!initResponse.IsSuccessStatusCode)
            {
                return new SyncOperationResult
                {
                    LocalId = venta.LocalId,
                    Success = false,
                    ErrorMessage = "Error al iniciar la venta en el servidor."
                };
            }

            var ventaCreada = await initResponse.Content.ReadFromJsonAsync<VentaDto>();
            if (ventaCreada is null)
            {
                return new SyncOperationResult
                {
                    LocalId = venta.LocalId,
                    Success = false,
                    ErrorMessage = "Respuesta inválida del servidor al crear venta."
                };
            }

            // Step 2: Add details
            foreach (var detalle in venta.Detalles)
            {
                var addDetalleRequest = new AddDetalleRequest
                {
                    ProductoId = detalle.ProductoId,
                    Cantidad = detalle.Cantidad
                };

                var detalleResponse = await _httpClient.PostAsJsonAsync(
                    $"api/ventas/{ventaCreada.Id}/detalles", addDetalleRequest);

                if (!detalleResponse.IsSuccessStatusCode)
                {
                    var errorContent = await detalleResponse.Content.ReadAsStringAsync();

                    // Detect stock conflict
                    if (errorContent.Contains("stock", StringComparison.OrdinalIgnoreCase)
                        || errorContent.Contains("insuficiente", StringComparison.OrdinalIgnoreCase))
                    {
                        return new SyncOperationResult
                        {
                            LocalId = venta.LocalId,
                            Success = false,
                            HasConflict = true,
                            ErrorMessage = $"Stock insuficiente para producto '{detalle.ProductoNombre}' " +
                                          $"(solicitado: {detalle.Cantidad})."
                        };
                    }

                    return new SyncOperationResult
                    {
                        LocalId = venta.LocalId,
                        Success = false,
                        ErrorMessage = $"Error al agregar detalle: {detalle.ProductoNombre}."
                    };
                }
            }

            // Step 3: Confirm the venta with payments
            var confirmRequest = new ConfirmVentaRequest
            {
                Pagos = venta.Pagos.Select(p => new PagoItemRequest
                {
                    MedioPagoId = p.MedioPagoId,
                    Monto = p.Monto
                }).ToList()
            };

            var confirmResponse = await _httpClient.PostAsJsonAsync(
                $"api/ventas/{ventaCreada.Id}/confirmar", confirmRequest);

            if (!confirmResponse.IsSuccessStatusCode)
            {
                var errorContent = await confirmResponse.Content.ReadAsStringAsync();

                if (errorContent.Contains("stock", StringComparison.OrdinalIgnoreCase)
                    || errorContent.Contains("insuficiente", StringComparison.OrdinalIgnoreCase))
                {
                    return new SyncOperationResult
                    {
                        LocalId = venta.LocalId,
                        Success = false,
                        HasConflict = true,
                        ErrorMessage = "Stock insuficiente al confirmar la venta."
                    };
                }

                return new SyncOperationResult
                {
                    LocalId = venta.LocalId,
                    Success = false,
                    ErrorMessage = "Error al confirmar la venta en el servidor."
                };
            }

            return new SyncOperationResult
            {
                LocalId = venta.LocalId,
                Success = true
            };
        }
        catch (HttpRequestException ex)
        {
            return new SyncOperationResult
            {
                LocalId = venta.LocalId,
                Success = false,
                ErrorMessage = $"Error de conexión: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            return new SyncOperationResult
            {
                LocalId = venta.LocalId,
                Success = false,
                ErrorMessage = $"Error inesperado: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Notifica al administrador sobre un conflicto de sincronización.
    /// Envía un POST al endpoint de notificaciones del servidor.
    /// </summary>
    private async Task NotifyConflictAsync(PendingVenta venta, string message)
    {
        try
        {
            var notification = new
            {
                Tipo = "ConflictoSincronizacion",
                VentaLocalId = venta.LocalId,
                Fecha = venta.FechaRegistro,
                Mensaje = message
            };

            await _httpClient.PostAsJsonAsync("api/notificaciones/conflicto-sync", notification);
        }
        catch
        {
            // Notification failure should not stop sync process
        }
    }

    /// <summary>
    /// Handler para cambios de conectividad. Auto-dispara sincronización al volver online.
    /// </summary>
    private async void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        if (e.IsOnline)
        {
            // Connection recovered - auto-trigger sync
            await SyncPendingOperationsAsync();
        }
        else
        {
            SetStatus(SyncStatus.Offline, "Conexión perdida.");
        }
    }

    private void SetStatus(SyncStatus status, string? message = null)
    {
        _currentStatus = status;
        var count = _offlineStorage.GetPendingCountAsync().GetAwaiter().GetResult();
        RaiseStatusChanged(status, count, message);
    }

    private void RaiseStatusChanged(SyncStatus status, int pendingCount, string? message)
    {
        StatusChanged?.Invoke(this, new SyncStatusChangedEventArgs(status, pendingCount, message));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _connectivityService.ConnectivityChanged -= OnConnectivityChanged;
            _disposed = true;
        }
    }
}
