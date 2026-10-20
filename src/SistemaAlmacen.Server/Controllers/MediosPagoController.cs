using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaAlmacen.Business.Interfaces;
using SistemaAlmacen.Shared.DTOs.MediosPago;

namespace SistemaAlmacen.Server.Controllers;

/// <summary>
/// Controller para gestión de medios de pago.
/// Lectura de activos disponible para Vendedor y Administrador; escritura restringida a Administrador.
/// </summary>
[ApiController]
[Route("api/medios-pago")]
[Authorize]
public class MediosPagoController : ControllerBase
{
    private readonly IMedioPagoService _medioPagoService;

    public MediosPagoController(IMedioPagoService medioPagoService)
    {
        _medioPagoService = medioPagoService;
    }

    /// <summary>
    /// Obtiene los medios de pago activos. Disponible para Vendedor y Administrador.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "RequireVendedorOrAdmin")]
    public async Task<ActionResult<List<MedioPagoDto>>> GetActivos()
    {
        var mediosPago = await _medioPagoService.GetActivosAsync();
        return Ok(mediosPago);
    }

    /// <summary>
    /// Obtiene los medios de pago ordenados por nombre. Sin filtro devuelve todos;
    /// ?activo=true solo activos, ?activo=false solo inhabilitados. Solo Administrador.
    /// </summary>
    [HttpGet("all")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<List<MedioPagoDto>>> GetAll([FromQuery] bool? activo)
    {
        var mediosPago = await _medioPagoService.GetAllAsync(activo);
        return Ok(mediosPago);
    }

    /// <summary>
    /// Crea un nuevo medio de pago. Solo Administrador.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<MedioPagoDto>> Create([FromBody] CreateMedioPagoRequest request)
    {
        var result = await _medioPagoService.CreateAsync(request);

        if (!result.IsSuccess)
        {
            if (result.HasFieldErrors)
                return BadRequest(new { errors = result.FieldErrors });

            return BadRequest(new { message = result.ErrorMessage });
        }

        return CreatedAtAction(nameof(GetActivos), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>
    /// Actualiza un medio de pago existente. Solo Administrador.
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<MedioPagoDto>> Update(int id, [FromBody] UpdateMedioPagoRequest request)
    {
        var result = await _medioPagoService.UpdateAsync(id, request);

        if (!result.IsSuccess)
        {
            if (result.HasFieldErrors)
                return BadRequest(new { errors = result.FieldErrors });

            if (result.ErrorCode == "NOT_FOUND")
                return NotFound(new { message = result.ErrorMessage });

            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Desactiva un medio de pago (no lo elimina). Solo Administrador.
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult> Deactivate(int id)
    {
        var result = await _medioPagoService.DeactivateAsync(id);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NOT_FOUND")
                return NotFound(new { message = result.ErrorMessage });

            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(new { message = "Medio de pago desactivado correctamente." });
    }

    /// <summary>
    /// Habilita o inhabilita un medio de pago. Solo Administrador.
    /// </summary>
    [HttpPatch("{id:int}/estado")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> SetEstado(int id, [FromBody] SistemaAlmacen.Shared.DTOs.CambiarEstadoRequest request)
    {
        var result = await _medioPagoService.SetActivoAsync(id, request.Activo);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NO_ENCONTRADO")
                return NotFound(new { message = result.ErrorMessage });

            return BadRequest(new { message = result.ErrorMessage });
        }

        var mensaje = request.Activo ? "Medio de pago habilitado correctamente." : "Medio de pago inhabilitado correctamente.";
        return Ok(new { message = mensaje });
    }
}
