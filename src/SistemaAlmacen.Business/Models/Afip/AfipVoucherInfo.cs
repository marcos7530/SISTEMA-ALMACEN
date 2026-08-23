namespace SistemaAlmacen.Business.Models.Afip;

/// <summary>
/// Información de un comprobante previamente emitido, consultado a AFIP/ARCA.
/// </summary>
public class AfipVoucherInfo
{
    /// <summary>
    /// Número de comprobante.
    /// </summary>
    public long Numero { get; set; }

    /// <summary>
    /// Código de Autorización Electrónico del comprobante.
    /// </summary>
    public string Cae { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de vencimiento del CAE.
    /// </summary>
    public DateTime CaeVencimiento { get; set; }

    /// <summary>
    /// Tipo de comprobante según tabla AFIP.
    /// </summary>
    public int TipoComprobante { get; set; }

    /// <summary>
    /// Importe total del comprobante.
    /// </summary>
    public decimal Total { get; set; }
}
