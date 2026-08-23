using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaAlmacen.Business.Interfaces;
using SistemaAlmacen.Shared.DTOs;
using SistemaAlmacen.Shared.DTOs.Auditoria;
using SistemaAlmacen.Shared.Enums;

namespace SistemaAlmacen.Server.Controllers;

/// <summary>
/// Controlador para consulta del historial de auditoría.
/// Acceso restringido a Administrador (Requirement 13.8).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "RequireAdmin")]
public class AuditoriaController : ControllerBase
{
    private readonly IAuditoriaService _auditoriaService;

    public AuditoriaController(IAuditoriaService auditoriaService)
    {
        _auditoriaService = auditoriaService;
    }

    /// <summary>
    /// Obtiene el historial de auditoría paginado y filtrado.
    /// Ordenado por fecha descendente (más reciente primero).
    /// </summary>
    /// <param name="usuarioId">Filtrar por usuario que realizó la operación.</param>
    /// <param name="fechaInicio">Filtrar desde esta fecha (inclusive).</param>
    /// <param name="fechaFin">Filtrar hasta esta fecha (inclusive).</param>
    /// <param name="tipoOperacion">Filtrar por tipo de operación.</param>
    /// <param name="entidadAfectada">Filtrar por entidad afectada.</param>
    /// <param name="page">Número de página (por defecto 1).</param>
    /// <param name="pageSize">Tamaño de página (por defecto 20).</param>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<AuditoriaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<AuditoriaDto>>> GetHistorial(
        [FromQuery] int? usuarioId,
        [FromQuery] DateTime? fechaInicio,
        [FromQuery] DateTime? fechaFin,
        [FromQuery] TipoOperacion? tipoOperacion,
        [FromQuery] string? entidadAfectada,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var filter = new AuditoriaFilter
        {
            UsuarioId = usuarioId,
            FechaInicio = fechaInicio,
            FechaFin = fechaFin,
            TipoOperacion = tipoOperacion,
            EntidadAfectada = entidadAfectada,
            Page = page,
            PageSize = pageSize
        };

        var result = await _auditoriaService.GetHistorialAsync(filter);
        return Ok(result);
    }
}
