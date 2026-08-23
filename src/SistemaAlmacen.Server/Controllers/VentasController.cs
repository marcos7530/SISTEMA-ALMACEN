using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaAlmacen.Business.Interfaces;
using SistemaAlmacen.Shared.DTOs;
using SistemaAlmacen.Shared.DTOs.Ventas;

namespace SistemaAlmacen.Server.Controllers;

/// <summary>
/// Controller para la gestión de ventas.
/// Acceso restringido a usuarios con rol Vendedor o Administrador.
/// </summary>
[ApiController]
[Route("api/ventas")]
[Authorize(Policy = "RequireVendedorOrAdmin")]
public class VentasController : ControllerBase
{
    private readonly IVentaService _ventaService;
    private readonly IFacturacionService _facturacionService;
    private readonly ILogger<VentasController> _logger;

    public VentasController(
        IVentaService ventaService,
        IFacturacionService facturacionService,
        ILogger<VentasController> logger)
    {
        _ventaService = ventaService;
        _facturacionService = facturacionService;
        _logger = logger;
    }

    /// <summary>
    /// Inicia una nueva venta en estado Borrador con el usuario actual como vendedor.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(VentaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VentaDto>> IniciarVenta()
    {
        var vendedorId = GetCurrentUserId();
        if (vendedorId is null)
            return BadRequest(new { message = "No se pudo identificar al usuario actual." });

        var venta = await _ventaService.InitializeVentaAsync(vendedorId.Value);
        return CreatedAtAction(nameof(GetVenta), new { id = venta.Id }, venta);
    }

    /// <summary>
    /// Agrega un detalle (producto con cantidad) a una venta existente.
    /// </summary>
    [HttpPost("{id}/detalles")]
    [ProducesResponseType(typeof(DetalleVentaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DetalleVentaDto>> AddDetalle(int id, [FromBody] AddDetalleRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _ventaService.AddDetalleAsync(id, request);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NOT_FOUND")
                return NotFound(new { message = result.ErrorMessage });

            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Elimina un detalle de una venta en estado Borrador.
    /// </summary>
    [HttpDelete("{id}/detalles/{detalleId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveDetalle(int id, int detalleId)
    {
        var result = await _ventaService.RemoveDetalleAsync(id, detalleId);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NOT_FOUND")
                return NotFound(new { message = result.ErrorMessage });

            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(new { message = "Detalle eliminado correctamente." });
    }

    /// <summary>
    /// Confirma una venta: valida stock, descuenta inventario, registra pagos y dispara facturación AFIP.
    /// </summary>
    [HttpPost("{id}/confirmar")]
    [ProducesResponseType(typeof(VentaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VentaDto>> ConfirmarVenta(int id, [FromBody] ConfirmVentaRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _ventaService.ConfirmVentaAsync(id, request);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NOT_FOUND")
                return NotFound(new { message = result.ErrorMessage });

            return BadRequest(new { message = result.ErrorMessage });
        }

        // Disparar facturación AFIP de forma no bloqueante.
        // Si falla, la venta queda como pendiente de facturación.
        _ = Task.Run(async () =>
        {
            try
            {
                await _facturacionService.EmitirComprobanteAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Facturación AFIP no pudo completarse para venta {VentaId}. Queda pendiente.", id);
            }
        });

        return Ok(result.Value);
    }

    /// <summary>
    /// Obtiene el historial de ventas paginado con filtros opcionales.
    /// Vendedor solo ve sus propias ventas; Administrador ve todas.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<VentaResumenDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<VentaResumenDto>>> GetHistorial(
        [FromQuery] DateTime? fechaInicio,
        [FromQuery] DateTime? fechaFin,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var filter = new VentaFilter
        {
            FechaInicio = fechaInicio,
            FechaFin = fechaFin,
            Page = page,
            PageSize = pageSize
        };

        // Si el usuario es Vendedor, forzar filtro por su propio ID
        if (IsVendedor())
        {
            var vendedorId = GetCurrentUserId();
            if (vendedorId is null)
                return BadRequest(new { message = "No se pudo identificar al usuario actual." });

            filter.VendedorId = vendedorId.Value;
        }

        var result = await _ventaService.GetHistorialAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Obtiene el detalle completo de una venta por su ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(VentaDetalleCompletoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VentaDetalleCompletoDto>> GetVenta(int id)
    {
        var venta = await _ventaService.GetDetalleCompletoAsync(id);

        if (venta is null)
            return NotFound(new { message = "Venta no encontrada." });

        return Ok(venta);
    }

    /// <summary>
    /// Extrae el ID del usuario actual desde los claims del JWT.
    /// </summary>
    private int? GetCurrentUserId()
    {
        var claim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)
                    ?? HttpContext.User.FindFirst("sub");

        if (claim is null || !int.TryParse(claim.Value, out var userId))
            return null;

        return userId;
    }

    /// <summary>
    /// Determina si el usuario actual tiene rol de Vendedor.
    /// </summary>
    private bool IsVendedor()
    {
        return HttpContext.User.IsInRole("Vendedor") && !HttpContext.User.IsInRole("Administrador");
    }
}
