using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaAlmacen.Business.Interfaces;
using SistemaAlmacen.Shared.DTOs;
using SistemaAlmacen.Shared.DTOs.Compras;

namespace SistemaAlmacen.Server.Controllers;

/// <summary>
/// Controller para el registro y gestión de compras de mercadería.
/// Requiere rol Administrador para operaciones de escritura y anulación.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ComprasController : ControllerBase
{
    private readonly ICompraService _compraService;

    public ComprasController(ICompraService compraService)
    {
        _compraService = compraService;
    }

    [HttpGet]
    [Authorize(Policy = "RequireVendedorOrAdmin")]
    public async Task<ActionResult<PaginatedResult<CompraResumenDto>>> GetHistorial(
        [FromQuery] int? proveedorId,
        [FromQuery] DateTime? fechaInicio,
        [FromQuery] DateTime? fechaFin,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var filter = new CompraFilter
        {
            ProveedorId = proveedorId,
            FechaInicio = fechaInicio,
            FechaFin = fechaFin,
            Page = page,
            PageSize = pageSize
        };

        var result = await _compraService.GetHistorialAsync(filter);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "RequireVendedorOrAdmin")]
    public async Task<ActionResult<CompraDto>> GetCompra(int id)
    {
        var compra = await _compraService.GetDetalleAsync(id);

        if (compra is null)
            return NotFound(new { message = "Compra no encontrada." });

        return Ok(compra);
    }

    [HttpGet("precio-sugerido")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<PrecioSugeridoDto>> GetPrecioSugerido(
        [FromQuery] int productoId,
        [FromQuery] decimal costoUnitario)
    {
        var result = await _compraService.CalcularPrecioSugeridoAsync(productoId, costoUnitario);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NOT_FOUND")
                return NotFound(new { message = result.ErrorMessage });
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(result.Value);
    }

    [HttpPost]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<CompraDto>> RegistrarCompra([FromBody] CreateCompraRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var usuarioId = GetCurrentUserId();
        if (usuarioId is null)
            return BadRequest(new { message = "No se pudo identificar al usuario actual." });

        var result = await _compraService.RegistrarCompraAsync(request, usuarioId.Value);

        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return CreatedAtAction(nameof(GetCompra), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPost("{id}/anular")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<CompraDto>> AnularCompra(int id)
    {
        var usuarioId = GetCurrentUserId();
        if (usuarioId is null)
            return BadRequest(new { message = "No se pudo identificar al usuario actual." });

        var result = await _compraService.AnularCompraAsync(id, usuarioId.Value);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NOT_FOUND")
                return NotFound(new { message = result.ErrorMessage });
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(result.Value);
    }

    private int? GetCurrentUserId()
    {
        var claim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)
                    ?? HttpContext.User.FindFirst("sub");

        if (claim is null || !int.TryParse(claim.Value, out var userId))
            return null;

        return userId;
    }
}
