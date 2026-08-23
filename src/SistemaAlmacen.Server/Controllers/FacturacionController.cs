using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SistemaAlmacen.Business.Interfaces;
using SistemaAlmacen.Shared.DTOs.Facturacion;

namespace SistemaAlmacen.Server.Controllers;

/// <summary>
/// Controlador de facturación electrónica AFIP/ARCA.
/// Gestiona configuración, emisión, reintentos y consulta de comprobantes.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FacturacionController : ControllerBase
{
    private readonly IFacturacionService _facturacionService;
    private readonly IAfipClientWrapper _afipClient;
    private readonly IConfiguration _configuration;

    public FacturacionController(
        IFacturacionService facturacionService,
        IAfipClientWrapper afipClient,
        IConfiguration configuration)
    {
        _facturacionService = facturacionService;
        _afipClient = afipClient;
        _configuration = configuration;
    }

    /// <summary>
    /// Obtiene la configuración actual de conexión AFIP.
    /// Solo accesible por Administrador. No expone datos sensibles (certificado, clave privada).
    /// </summary>
    [HttpGet("config")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(typeof(AfipConfigDto), StatusCodes.Status200OK)]
    public ActionResult<AfipConfigDto> GetConfig()
    {
        var config = new AfipConfigDto
        {
            Cuit = _configuration.GetValue<string>("Afip:Cuit") ?? string.Empty,
            PuntoDeVenta = _configuration.GetValue<int>("Afip:PuntoDeVenta", 1),
            ModoProduccion = _configuration.GetValue<bool>("Afip:ModoProduccion", false),
            TieneCertificado = !string.IsNullOrWhiteSpace(_configuration.GetValue<string>("Afip:Certificado")),
            UsaMock = _configuration.GetValue<bool>("Afip:UseMock", true)
        };

        return Ok(config);
    }

    /// <summary>
    /// Emite un comprobante electrónico AFIP para una venta confirmada.
    /// </summary>
    /// <param name="ventaId">ID de la venta a facturar.</param>
    [HttpPost("emitir/{ventaId:int}")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(typeof(ComprobanteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ComprobanteDto>> Emitir(int ventaId)
    {
        var result = await _facturacionService.EmitirComprobanteAsync(ventaId);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage, code = result.ErrorCode });

        return Ok(result.Value);
    }

    /// <summary>
    /// Reintenta la emisión de comprobante para una venta pendiente de facturación.
    /// </summary>
    /// <param name="ventaId">ID de la venta pendiente.</param>
    [HttpPost("reintentar/{ventaId:int}")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(typeof(ComprobanteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ComprobanteDto>> Reintentar(int ventaId)
    {
        var result = await _facturacionService.ReintentarEmisionAsync(ventaId);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage, code = result.ErrorCode });

        return Ok(result.Value);
    }

    /// <summary>
    /// Reintenta la emisión masiva de todos los comprobantes pendientes.
    /// </summary>
    [HttpPost("reintentar-masivo")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReintentarMasivo()
    {
        var result = await _facturacionService.EmitirPendientesMasivamenteAsync();

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage, code = result.ErrorCode });

        return Ok(new { message = "Emisión masiva completada exitosamente." });
    }

    /// <summary>
    /// Obtiene la lista de ventas pendientes de facturación.
    /// </summary>
    [HttpGet("pendientes")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(typeof(List<VentaPendienteFacturacionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<VentaPendienteFacturacionDto>>> GetPendientes()
    {
        var pendientes = await _facturacionService.GetPendientesAsync();
        return Ok(pendientes);
    }

    /// <summary>
    /// Consulta un comprobante emitido para una venta.
    /// </summary>
    /// <param name="ventaId">ID de la venta.</param>
    [HttpGet("comprobante/{ventaId:int}")]
    [ProducesResponseType(typeof(ComprobanteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ComprobanteDto>> GetComprobante(int ventaId)
    {
        var comprobante = await _facturacionService.ConsultarComprobanteAsync(ventaId);

        if (comprobante is null)
            return NotFound(new { error = "No se encontró comprobante para esta venta." });

        return Ok(comprobante);
    }

    /// <summary>
    /// Genera y descarga el PDF de un comprobante fiscal.
    /// </summary>
    /// <param name="ventaId">ID de la venta.</param>
    [HttpGet("comprobante/{ventaId:int}/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetComprobantePdf(int ventaId)
    {
        var comprobante = await _facturacionService.ConsultarComprobanteAsync(ventaId);

        if (comprobante is null)
            return NotFound(new { error = "No se encontró comprobante para esta venta." });

        var pdf = await _facturacionService.GenerarPdfComprobanteAsync(ventaId);

        if (pdf.Length == 0)
            return BadRequest(new { error = "La generación de PDF no está disponible aún." });

        return File(pdf, "application/pdf", $"comprobante_{ventaId}.pdf");
    }

    /// <summary>
    /// Prueba la conexión con los Web Services de AFIP.
    /// Solo accesible por Administrador.
    /// </summary>
    [HttpGet("test-connection")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> TestConnection()
    {
        var isConnected = await _afipClient.TestConnectionAsync();

        return Ok(new
        {
            connected = isConnected,
            mode = _configuration.GetValue<bool>("Afip:UseMock", true) ? "mock" : "produccion"
        });
    }
}
