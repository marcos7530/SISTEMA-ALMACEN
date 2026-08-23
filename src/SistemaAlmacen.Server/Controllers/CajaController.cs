using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaAlmacen.Business.Interfaces;
using SistemaAlmacen.Shared.DTOs;
using SistemaAlmacen.Shared.DTOs.Caja;

namespace SistemaAlmacen.Server.Controllers;

/// <summary>
/// Controlador para gestión de caja: apertura, cierre, retiros, ingresos y consultas.
/// </summary>
[ApiController]
[Route("api/caja")]
[Authorize]
public class CajaController : ControllerBase
{
    private readonly ICajaService _cajaService;

    public CajaController(ICajaService cajaService)
    {
        _cajaService = cajaService;
    }

    /// <summary>
    /// Abre una nueva caja con el monto inicial indicado.
    /// Solo Vendedor o Administrador pueden abrir caja.
    /// </summary>
    [HttpPost("abrir")]
    [Authorize(Policy = "RequireVendedorOrAdmin")]
    [ProducesResponseType(typeof(CajaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CajaDto>> AbrirCaja([FromBody] AbrirCajaRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return BadRequest(new { message = "No se pudo identificar al usuario actual." });

        var result = await _cajaService.AbrirCajaAsync(request, userId.Value);

        if (!result.IsSuccess)
        {
            if (result.HasFieldErrors)
                return BadRequest(new { errors = result.FieldErrors });

            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Cierra la caja abierta actualmente. Calcula saldo esperado y diferencia.
    /// Solo Vendedor o Administrador pueden cerrar caja.
    /// </summary>
    [HttpPost("cerrar")]
    [Authorize(Policy = "RequireVendedorOrAdmin")]
    [ProducesResponseType(typeof(CierreResumenDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CierreResumenDto>> CerrarCaja([FromBody] CerrarCajaRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return BadRequest(new { message = "No se pudo identificar al usuario actual." });

        var result = await _cajaService.CerrarCajaAsync(request, userId.Value);

        if (!result.IsSuccess)
        {
            if (result.HasFieldErrors)
                return BadRequest(new { errors = result.FieldErrors });

            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Registra un retiro de efectivo de la caja. Solo Administrador.
    /// </summary>
    [HttpPost("retiro")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(typeof(MovimientoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MovimientoDto>> RegistrarRetiro([FromBody] MovimientoRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return BadRequest(new { message = "No se pudo identificar al usuario actual." });

        var result = await _cajaService.RegistrarRetiroAsync(request, userId.Value);

        if (!result.IsSuccess)
        {
            if (result.HasFieldErrors)
                return BadRequest(new { errors = result.FieldErrors });

            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Registra un ingreso adicional de efectivo a la caja. Solo Administrador.
    /// </summary>
    [HttpPost("ingreso")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(typeof(MovimientoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MovimientoDto>> RegistrarIngreso([FromBody] MovimientoRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return BadRequest(new { message = "No se pudo identificar al usuario actual." });

        var result = await _cajaService.RegistrarIngresoAsync(request, userId.Value);

        if (!result.IsSuccess)
        {
            if (result.HasFieldErrors)
                return BadRequest(new { errors = result.FieldErrors });

            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Obtiene la caja actualmente abierta para un punto de venta.
    /// </summary>
    [HttpGet("actual")]
    [Authorize(Policy = "RequireVendedorOrAdmin")]
    [ProducesResponseType(typeof(CajaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CajaDto>> GetCajaActual([FromQuery] int puntoDeVentaId = 1)
    {
        var caja = await _cajaService.GetCajaAbiertaAsync(puntoDeVentaId);

        if (caja is null)
            return NotFound(new { message = "No hay una caja abierta para el punto de venta indicado." });

        return Ok(caja);
    }

    /// <summary>
    /// Obtiene el historial paginado de cierres de caja con filtros opcionales.
    /// Solo Administrador puede consultar el historial.
    /// </summary>
    [HttpGet("historial-cierres")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(typeof(PaginatedResult<CierreResumenDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<CierreResumenDto>>> GetHistorialCierres(
        [FromQuery] DateTime? fechaInicio,
        [FromQuery] DateTime? fechaFin,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var filter = new CierreFilter
        {
            FechaInicio = fechaInicio,
            FechaFin = fechaFin,
            Page = page,
            PageSize = pageSize
        };

        var result = await _cajaService.GetHistorialCierresAsync(filter);
        return Ok(result);
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
}
