namespace SistemaAlmacen.Client.Offline;

/// <summary>
/// Represents a sale registered while offline, pending synchronization with the server.
/// </summary>
public class PendingVenta
{
    /// <summary>
    /// Local unique identifier (GUID) for tracking before server sync.
    /// </summary>
    public string LocalId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Timestamp when the sale was recorded offline.
    /// </summary>
    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Alias for Fecha, kept for backward compatibility.
    /// </summary>
    public DateTime FechaRegistro
    {
        get => Fecha;
        set => Fecha = value;
    }

    /// <summary>
    /// The line items of the sale.
    /// </summary>
    public List<PendingVentaDetalle> Detalles { get; set; } = new();

    /// <summary>
    /// Payment information for the sale.
    /// </summary>
    public List<PendingVentaPago> Pagos { get; set; } = new();

    /// <summary>
    /// Calculated total of all line items.
    /// </summary>
    public decimal Total { get; set; }
}

/// <summary>
/// A line item within a pending offline sale.
/// </summary>
public class PendingVentaDetalle
{
    public int ProductoId { get; set; }
    public string ProductoNombre { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}

/// <summary>
/// Payment method applied to a pending offline sale.
/// </summary>
public class PendingVentaPago
{
    public int MedioPagoId { get; set; }
    public string MedioPagoNombre { get; set; } = string.Empty;
    public decimal Monto { get; set; }
}
