using SistemaAlmacen.Business.Models.Afip;

namespace SistemaAlmacen.Business.Interfaces;

/// <summary>
/// Wrapper de abstracción para la conexión con los Web Services de AFIP/ARCA.
/// Permite desacoplar la lógica de negocio del SDK específico de AFIP y facilita
/// el uso de un stub/mock en desarrollo.
/// </summary>
public interface IAfipClientWrapper
{
    /// <summary>
    /// Solicita la emisión del próximo comprobante a AFIP (CAE).
    /// </summary>
    /// <param name="request">Datos del comprobante a emitir.</param>
    /// <returns>Respuesta de AFIP con CAE o errores.</returns>
    Task<AfipVoucherResponse> CreateNextVoucherAsync(AfipVoucherRequest request);

    /// <summary>
    /// Consulta la información de un comprobante previamente emitido en AFIP.
    /// </summary>
    /// <param name="puntoDeVenta">Punto de venta del comprobante.</param>
    /// <param name="tipoComprobante">Tipo de comprobante según tabla AFIP.</param>
    /// <param name="numero">Número de comprobante.</param>
    /// <returns>Información del comprobante, o null si no se encontró.</returns>
    Task<AfipVoucherInfo?> GetVoucherInfoAsync(int puntoDeVenta, int tipoComprobante, long numero);

    /// <summary>
    /// Verifica la conectividad con los Web Services de AFIP.
    /// </summary>
    /// <returns>True si la conexión fue exitosa.</returns>
    Task<bool> TestConnectionAsync();
}
