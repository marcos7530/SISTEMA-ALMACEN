using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaAlmacen.Business.Interfaces;
using SistemaAlmacen.Shared.DTOs.CuentaCorriente;

namespace SistemaAlmacen.Server.Controllers;

/// <summary>
/// Controller para la gestión de cuenta corriente de clientes.
/// Consulta disponible para Vendedor y Administrador; registro de cobros solo Administrador.
/// </summary>
[ApiController]
[Route("api/cuenta-corriente")]
[Authorize]
public class CuentaCorrienteController : ControllerBase
{
    private readonly ICuentaCorrienteService _cuentaCorrienteService;

    public CuentaCorrienteController(ICuentaCorrienteService cuentaCorrienteService)
    {
        _cuentaCorrienteService = cuentaCorrienteService;
    }

    /// <summary>
    /// Obtiene el estado de cuenta corriente de un cliente: saldo y movimientos.
    /// </summary>
    [HttpGet("{clienteId}")]
    [Authorize(Policy = "RequireVendedorOrAdmin")]
    public async Task<ActionResult<EstadoCuentaCorrienteDto>> GetEstadoCuenta(int clienteId)
    {
        var result = await _cuentaCorrienteService.GetEstadoCuentaAsync(clienteId);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NOT_FOUND")
                return NotFound(new { message = result.ErrorMessage });
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Registra un cobro/pago recibido de un cliente. Solo Administrador.
    /// </summary>
    [HttpPost("{clienteId}/pagos")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<MovimientoCuentaCorrienteDto>> RegistrarPago(
        int clienteId, [FromBody] RegistrarPagoRequest request)
    {
        var usuarioId = GetCurrentUserId();
        if (usuarioId is null)
            return Unauthorized(new { message = "No se pudo identificar al usuario." });

        var result = await _cuentaCorrienteService.RegistrarPagoAsync(clienteId, request, usuarioId.Value);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NOT_FOUND")
                return NotFound(new { message = result.ErrorMessage });
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(result.Value);
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
