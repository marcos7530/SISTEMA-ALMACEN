namespace SistemaAlmacen.Shared.DTOs.Facturacion;

/// <summary>
/// DTO de configuración de conexión AFIP.
/// No expone datos sensibles como certificado o clave privada.
/// Solo indica si están configurados.
/// </summary>
public class AfipConfigDto
{
    /// <summary>
    /// CUIT del contribuyente configurado.
    /// </summary>
    public string Cuit { get; set; } = string.Empty;

    /// <summary>
    /// Punto de venta AFIP configurado.
    /// </summary>
    public int PuntoDeVenta { get; set; }

    /// <summary>
    /// Indica si el sistema está en modo producción (true) o desarrollo/homologación (false).
    /// </summary>
    public bool ModoProduccion { get; set; }

    /// <summary>
    /// Indica si hay un certificado digital configurado (no expone el certificado en sí).
    /// </summary>
    public bool TieneCertificado { get; set; }

    /// <summary>
    /// Indica si el sistema está usando el mock de AFIP (modo desarrollo sin conexión real).
    /// </summary>
    public bool UsaMock { get; set; }
}
