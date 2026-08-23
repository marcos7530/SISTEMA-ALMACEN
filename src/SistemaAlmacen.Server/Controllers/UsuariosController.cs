using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaAlmacen.Business.Interfaces;
using SistemaAlmacen.Shared.DTOs;
using SistemaAlmacen.Shared.DTOs.Usuarios;

namespace SistemaAlmacen.Server.Controllers;

/// <summary>
/// Controlador para gestión de usuarios. Acceso restringido a Administrador.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "RequireAdmin")]
public class UsuariosController : ControllerBase
{
    private readonly IUsuarioService _usuarioService;

    public UsuariosController(IUsuarioService usuarioService)
    {
        _usuarioService = usuarioService;
    }

    /// <summary>
    /// Obtiene la lista paginada de usuarios activos con búsqueda opcional.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<UsuarioDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<UsuarioDto>>> GetUsuarios(
        [FromQuery] string? searchTerm,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var filter = new UsuarioFilter
        {
            SearchTerm = searchTerm,
            Page = page,
            PageSize = pageSize
        };

        var result = await _usuarioService.GetUsuariosAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Obtiene un usuario por su ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UsuarioDto>> GetById(int id)
    {
        var usuario = await _usuarioService.GetByIdAsync(id);

        if (usuario is null)
            return NotFound(new { message = "Usuario no encontrado." });

        return Ok(usuario);
    }

    /// <summary>
    /// Crea un nuevo usuario.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UsuarioDto>> Create([FromBody] CreateUsuarioRequest request)
    {
        var result = await _usuarioService.CreateAsync(request);

        if (!result.IsSuccess)
        {
            if (result.HasFieldErrors)
                return BadRequest(new { errors = result.FieldErrors });

            return BadRequest(new { message = result.ErrorMessage });
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>
    /// Actualiza un usuario existente.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UsuarioDto>> Update(int id, [FromBody] UpdateUsuarioRequest request)
    {
        var result = await _usuarioService.UpdateAsync(id, request);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NOT_FOUND")
                return NotFound(new { message = result.ErrorMessage });

            if (result.HasFieldErrors)
                return BadRequest(new { errors = result.FieldErrors });

            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Desactiva (eliminación lógica) un usuario.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var currentUserId = GetCurrentUserId();

        if (currentUserId is null)
            return BadRequest(new { message = "No se pudo identificar al usuario actual." });

        var result = await _usuarioService.DeactivateAsync(id, currentUserId.Value);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NOT_FOUND")
                return NotFound(new { message = result.ErrorMessage });

            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(new { message = "Usuario desactivado exitosamente." });
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
