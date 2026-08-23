using SistemaAlmacen.Shared.DTOs.Reportes;

namespace SistemaAlmacen.Business.Interfaces;

/// <summary>
/// Servicio para generación de reportes de ventas, productos más vendidos e inventario.
/// </summary>
public interface IReporteService
{
    /// <summary>
    /// Genera un reporte de ventas confirmadas dentro del rango de fechas especificado.
    /// Incluye total monetario, cantidad de transacciones, desglose diario y desglose por medio de pago.
    /// </summary>
    /// <param name="fechaInicio">Fecha de inicio del período (inclusive).</param>
    /// <param name="fechaFin">Fecha de fin del período (inclusive).</param>
    /// <returns>Resumen de ventas con desgloses diario y por medio de pago.</returns>
    /// <exception cref="ArgumentException">Si el rango excede 365 días.</exception>
    Task<ReporteVentasDto> GenerarReporteVentasAsync(DateTime fechaInicio, DateTime fechaFin);

    /// <summary>
    /// Genera un listado de los 50 productos con mayor cantidad total de unidades vendidas
    /// en el período indicado, ordenados de mayor a menor.
    /// </summary>
    /// <param name="fechaInicio">Fecha de inicio del período (inclusive).</param>
    /// <param name="fechaFin">Fecha de fin del período (inclusive).</param>
    /// <returns>Lista de hasta 50 productos más vendidos con nombre, categoría y cantidad.</returns>
    Task<List<ProductoMasVendidoDto>> GenerarReporteProductosAsync(DateTime fechaInicio, DateTime fechaFin);

    /// <summary>
    /// Genera un listado de todos los productos registrados (activos e inactivos)
    /// con nombre, categoría, stock actual, precio y estado.
    /// </summary>
    /// <returns>Lista completa de productos del inventario.</returns>
    Task<List<ReporteInventarioDto>> GenerarReporteInventarioAsync();

    /// <summary>
    /// Exporta un reporte al formato solicitado (PDF, Excel, CSV).
    /// Placeholder: la implementación real se realizará en la tarea 17.2.
    /// </summary>
    /// <param name="request">Datos de exportación: formato, tipo de reporte y rango de fechas.</param>
    /// <returns>Contenido del archivo exportado como byte array.</returns>
    Task<byte[]> ExportarAsync(ExportRequest request);
}
