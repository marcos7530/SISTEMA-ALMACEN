using Microsoft.JSInterop;

namespace SistemaAlmacen.Client.Offline;

/// <summary>
/// Detects connection state using navigator.onLine and browser online/offline events via JS Interop.
/// </summary>
public class ConnectivityService : IConnectivityService
{
    private readonly IJSRuntime _jsRuntime;
    private DotNetObjectReference<ConnectivityService>? _dotNetRef;
    private bool _isOnline = true;

    public bool IsOnline => _isOnline;

    public event EventHandler<ConnectivityChangedEventArgs>? ConnectivityChanged;

    public ConnectivityService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task InitializeAsync()
    {
        _dotNetRef = DotNetObjectReference.Create(this);
        _isOnline = await _jsRuntime.InvokeAsync<bool>("offlineInterop.isOnline");
        await _jsRuntime.InvokeVoidAsync("offlineInterop.registerConnectivityHandler", _dotNetRef);
    }

    /// <summary>
    /// Called from JavaScript when connectivity state changes.
    /// </summary>
    [JSInvokable]
    public void OnConnectivityChanged(bool isOnline)
    {
        _isOnline = isOnline;
        ConnectivityChanged?.Invoke(this, new ConnectivityChangedEventArgs(isOnline));
    }

    public async ValueTask DisposeAsync()
    {
        if (_dotNetRef is not null)
        {
            await _jsRuntime.InvokeVoidAsync("offlineInterop.unregisterConnectivityHandler");
            _dotNetRef.Dispose();
            _dotNetRef = null;
        }
    }
}
