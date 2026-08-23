namespace SistemaAlmacen.Client.Offline;

/// <summary>
/// Event args for connectivity change events.
/// </summary>
public class ConnectivityChangedEventArgs : EventArgs
{
    public bool IsOnline { get; }

    public ConnectivityChangedEventArgs(bool isOnline)
    {
        IsOnline = isOnline;
    }
}

/// <summary>
/// Service to detect network connection state and notify on changes.
/// </summary>
public interface IConnectivityService : IAsyncDisposable
{
    /// <summary>
    /// Gets whether the application currently has a network connection.
    /// </summary>
    bool IsOnline { get; }

    /// <summary>
    /// Raised when the connectivity state changes.
    /// </summary>
    event EventHandler<ConnectivityChangedEventArgs>? ConnectivityChanged;

    /// <summary>
    /// Initializes the connectivity monitoring (subscribes to browser events).
    /// Must be called once after the service is injected.
    /// </summary>
    Task InitializeAsync();
}
