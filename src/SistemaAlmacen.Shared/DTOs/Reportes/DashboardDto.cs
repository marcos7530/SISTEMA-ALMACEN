namespace SistemaAlmacen.Shared.DTOs.Reportes;

/// <summary>
/// DTO con las métricas clave del negocio para el dashboard principal.
/// </summary>
public class DashboardDto
{
    // --- Ventas de hoy ---
    public decimal VentasHoyMonto { get; set; }
    public int VentasHoyTransacciones { get; set; }
    public decimal TicketPromedioHoy { get; set; }

    // --- Ventas del mes actual ---
    public decimal VentasMesMonto { get; set; }
    public int VentasMesTransacciones { get; set; }

    // --- Inventario ---
    /// <summary>Cantidad de productos activos con stock por debajo del umbral (pero mayor a 0).</summary>
    public int ProductosStockBajo { get; set; }

    /// <summary>Cantidad de productos activos sin stock (stock = 0).</summary>
    public int ProductosAgotados { get; set; }

    /// <summary>Valor total del inventario a precio de costo.</summary>
    public decimal ValorInventarioCosto { get; set; }

    /// <summary>Valor total del inventario a precio de venta.</summary>
    public decimal ValorInventarioVenta { get; set; }

    // --- Facturación ---
    /// <summary>Cantidad de comprobantes que quedaron pendientes o rechazados por AFIP.</summary>
    public int ComprobantesPendientes { get; set; }

    // --- Top productos del mes ---
    public List<ProductoMasVendidoDto> TopProductosMes { get; set; } = new();

    /// <summary>Umbral de stock bajo usado en el cálculo (para mostrarlo en la UI).</summary>
    public int StockBajoUmbral { get; set; }
}
