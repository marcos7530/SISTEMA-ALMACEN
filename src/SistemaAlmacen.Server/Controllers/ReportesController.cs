using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaAlmacen.Business.Interfaces;
using SistemaAlmacen.Shared.DTOs.Reportes;
using SistemaAlmacen.Shared.Enums;

namespace SistemaAlmacen.Server.Controllers;

/// <summary>
/// Controller para la generación y exportación de reportes.
/// Acceso restringido a Administrador, excepto reporte de ventas propias para Vendedor.
/// Requirements: 11.1, 6.2, 6.3
/// </summary>
[ApiController]
[Route("api/reportes")]
[Authorize]
public class ReportesController : ControllerBase
{
    private readonly IReporteService _reporteService;

    public ReportesController(IReporteService reporteService)
    {
        _reporteService = reporteService;
    }

    /// <summary>
    /// Genera un reporte de ventas para el rango de fechas indicado.
    /// Administrador ve todas las ventas; Vendedor ve solo sus propias ventas.
    /// </summary>
    /// <param name="fechaInicio">Fecha de inicio del período (inclusive).</param>
    /// <param name="fechaFin">Fecha de fin del período (inclusive).</param>
    [HttpGet("ventas")]
    [Authorize(Policy = "RequireVendedorOrAdmin")]
    [ProducesResponseType(typeof(ReporteVentasDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReporteVentasDto>> GetReporteVentas(
        [FromQuery] DateTime fechaInicio,
        [FromQuery] DateTime fechaFin)
    {
        try
        {
            var result = await _reporteService.GenerarReporteVentasAsync(fechaInicio, fechaFin);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Genera un reporte de los 50 productos más vendidos en el período.
    /// Acceso restringido a Administrador.
    /// </summary>
    /// <param name="fechaInicio">Fecha de inicio del período (inclusive).</param>
    /// <param name="fechaFin">Fecha de fin del período (inclusive).</param>
    [HttpGet("productos-mas-vendidos")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(typeof(List<ProductoMasVendidoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<ProductoMasVendidoDto>>> GetProductosMasVendidos(
        [FromQuery] DateTime fechaInicio,
        [FromQuery] DateTime fechaFin)
    {
        try
        {
            var result = await _reporteService.GenerarReporteProductosAsync(fechaInicio, fechaFin);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Genera un reporte de inventario con todos los productos (activos e inactivos).
    /// Acceso restringido a Administrador.
    /// </summary>
    [HttpGet("inventario")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(typeof(List<ReporteInventarioDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ReporteInventarioDto>>> GetReporteInventario()
    {
        var result = await _reporteService.GenerarReporteInventarioAsync();
        return Ok(result);
    }

    /// <summary>
    /// Exporta un reporte al formato solicitado (PDF, Excel, CSV).
    /// Retorna el archivo para descarga.
    /// Acceso restringido a Administrador.
    /// </summary>
    /// <param name="request">Datos de exportación: formato, tipo de reporte y rango de fechas.</param>
    [HttpPost("exportar")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Exportar([FromBody] ExportRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var fileContent = await _reporteService.ExportarAsync(request);

            var contentType = GetContentType(request.Formato);
            var fileName = GenerateFileName(request);

            return File(fileContent, contentType, fileName);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Obtiene el content type apropiado según el formato de exportación.
    /// </summary>
    private static string GetContentType(FormatoExportacion formato) => formato switch
    {
        FormatoExportacion.PDF => "application/pdf",
        FormatoExportacion.Excel => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        FormatoExportacion.CSV => "text/csv",
        _ => "application/octet-stream"
    };

    /// <summary>
    /// Genera el nombre del archivo de exportación con tipo de reporte y rango de fechas.
    /// </summary>
    private static string GenerateFileName(ExportRequest request)
    {
        var extension = request.Formato switch
        {
            FormatoExportacion.PDF => "pdf",
            FormatoExportacion.Excel => "xlsx",
            FormatoExportacion.CSV => "csv",
            _ => "bin"
        };

        var fechaInicioStr = request.FechaInicio.ToString("yyyyMMdd");
        var fechaFinStr = request.FechaFin.ToString("yyyyMMdd");

        return $"{request.TipoReporte}_{fechaInicioStr}_{fechaFinStr}.{extension}";
    }
}
