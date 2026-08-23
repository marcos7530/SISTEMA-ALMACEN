namespace SistemaAlmacen.Business.Models.Afip;

/// <summary>
/// Modelo de solicitud para emitir un comprobante a través de AFIP/ARCA.
/// Contiene los datos necesarios para la solicitud de CAE.
/// </summary>
public class AfipVoucherRequest
{
    /// <summary>
    /// Punto de venta habilitado en AFIP.
    /// </summary>
    public int PuntoDeVenta { get; set; }

    /// <summary>
    /// Tipo de comprobante según tabla AFIP: Factura A (1), Factura B (6), Factura C (11).
    /// </summary>
    public int TipoComprobante { get; set; }

    /// <summary>
    /// Importe total del comprobante.
    /// </summary>
    public decimal Total { get; set; }

    /// <summary>
    /// Importe neto gravado (base imponible para IVA).
    /// </summary>
    public decimal NetoGravado { get; set; }

    /// <summary>
    /// Importe de IVA.
    /// </summary>
    public decimal Iva { get; set; }

    /// <summary>
    /// Importe exento de IVA.
    /// </summary>
    public decimal Exento { get; set; }

    /// <summary>
    /// Código de moneda. Por defecto "PES" (Pesos Argentinos).
    /// </summary>
    public string Moneda { get; set; } = "PES";

    /// <summary>
    /// Fecha del comprobante.
    /// </summary>
    public DateTime FechaComprobante { get; set; }
}
