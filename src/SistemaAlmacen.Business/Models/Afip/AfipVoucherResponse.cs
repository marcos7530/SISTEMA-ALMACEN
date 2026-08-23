namespace SistemaAlmacen.Business.Models.Afip;

/// <summary>
/// Modelo de respuesta de AFIP/ARCA tras solicitar un comprobante.
/// Contiene el CAE si fue exitoso, o los errores en caso contrario.
/// </summary>
public class AfipVoucherResponse
{
    /// <summary>
    /// Indica si AFIP emitió un CAE válido para el comprobante.
    /// </summary>
    public bool HasCae { get; set; }

    /// <summary>
    /// Código de Autorización Electrónico (14 dígitos). Null si no se obtuvo.
    /// </summary>
    public string? Cae { get; set; }

    /// <summary>
    /// Fecha de vencimiento del CAE. Null si no se obtuvo.
    /// </summary>
    public DateTime? CaeVencimiento { get; set; }

    /// <summary>
    /// Número de comprobante asignado por AFIP. Null si no se obtuvo.
    /// </summary>
    public long? NumeroComprobante { get; set; }

    /// <summary>
    /// Mensaje de error principal. Null si fue exitoso.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Lista de errores detallados retornados por AFIP. Null si fue exitoso.
    /// </summary>
    public List<string>? Errors { get; set; }
}
