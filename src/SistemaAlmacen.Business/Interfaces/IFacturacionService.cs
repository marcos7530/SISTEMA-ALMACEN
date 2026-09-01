using SistemaAlmacen.Shared.Common;
using SistemaAlmacen.Shared.DTOs.Facturacion;

namespace SistemaAlmacen.Business.Interfaces;

/// <summary>
/// Servicio de facturación electrónica AFIP/ARCA.
/// Maneja la emisión de comprobantes, reintentos y consulta de pendientes.
/// </summary>
public interface IFacturacionService
{
    /// <summary>
    /// Emite un comprobante electrónico AFIP para la venta indicada.
    /// Si falla, la venta queda marcada como pendiente de facturación.
    /// </summary>
    /// <param name="ventaId">ID de la venta a facturar.</param>
    /// <returns>Resultado con el comprobante emitido o error.</returns>
    Task<Result<ComprobanteDto>> EmitirComprobanteAsync(int ventaId);

    /// <summary>
    /// Reintenta la emisión de comprobante para una venta pendiente de facturación.
    /// </summary>
    /// <param name="ventaId">ID de la venta pendiente.</param>
    /// <returns>Resultado con el comprobante emitido o error.</returns>
    Task<Result<ComprobanteDto>> ReintentarEmisionAsync(int ventaId);

    /// <summary>
    /// Obtiene la lista de ventas pendientes de facturación.
    /// </summary>
    /// <returns>Lista de ventas pendientes con detalle del error.</returns>
    Task<List<VentaPendienteFacturacionDto>> GetPendientesAsync();

    /// <summary>
    /// Reintenta la emisión de comprobantes para todas las ventas pendientes.
    /// </summary>
    /// <returns>Resultado indicando si la operación masiva fue exitosa.</returns>
    Task<Result> EmitirPendientesMasivamenteAsync();

    /// <summary>
    /// Consulta los datos de un comprobante emitido para una venta.
    /// </summary>
    /// <param name="ventaId">ID de la venta.</param>
    /// <returns>Datos del comprobante, o null si no existe.</returns>
    Task<ComprobanteDto?> ConsultarComprobanteAsync(int ventaId);

    /// <summary>
    /// Genera el PDF del comprobante fiscal para una venta.
    /// </summary>
    /// <param name="ventaId">ID de la venta.</param>
    /// <returns>Bytes del archivo PDF.</returns>
    Task<byte[]> GenerarPdfComprobanteAsync(int ventaId);

    /// <summary>
    /// Emite una nota de crédito electrónica AFIP asociada a la factura de una venta anulada.
    /// El tipo de NC (A/B/C) se determina según el tipo de la factura original.
    /// </summary>
    /// <param name="ventaId">ID de la venta cuya factura se anula con nota de crédito.</param>
    /// <returns>Resultado con la nota de crédito emitida o error.</returns>
    Task<Result<ComprobanteDto>> EmitirNotaCreditoAsync(int ventaId);
}
