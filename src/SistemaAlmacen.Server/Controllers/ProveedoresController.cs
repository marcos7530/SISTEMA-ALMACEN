using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaAlmacen.Business.Interfaces;
using SistemaAlmacen.Shared.DTOs;
using SistemaAlmacen.Shared.DTOs.Proveedores;

namespace SistemaAlmacen.Server.Controllers;

/// <summary>
/// Controller para la gestión de proveedores.
/// Lectura para Vendedor/Administrador; escritura solo Administrador.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProveedoresController : ControllerBase
{
    private readonly IProveedorService _proveedorService;

    public ProveedoresController(IProveedorService proveedorService)
    {
        _proveedorService = proveedorService;
    }

    [HttpGet]
    [Authorize(Policy = "RequireVendedorOrAdmin")]
    public async Task<ActionResult<PaginatedResult<ProveedorDto>>> GetProveedores(
        [FromQuery] string? searchTerm,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var filter = new ProveedorFilter
        {
            SearchTerm = searchTerm,
            Page = page,
            PageSize = pageSize
        };

        var result = await _proveedorService.GetProveedoresAsync(filter);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "RequireVendedorOrAdmin")]
    public async Task<ActionResult<ProveedorDto>> GetProveedor(int id)
    {
        var proveedor = await _proveedorService.GetByIdAsync(id);

        if (proveedor is null)
            return NotFound(new { message = "Proveedor no encontrado." });

        return Ok(proveedor);
    }

    [HttpPost]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<ProveedorDto>> CreateProveedor([FromBody] CreateProveedorRequest request)
    {
        var result = await _proveedorService.CreateAsync(request);

        if (!result.IsSuccess)
        {
            if (result.HasFieldErrors)
                return BadRequest(new { message = result.ErrorMessage, errors = result.FieldErrors });
            return BadRequest(new { message = result.ErrorMessage });
        }

        return CreatedAtAction(nameof(GetProveedor), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<ProveedorDto>> UpdateProveedor(int id, [FromBody] UpdateProveedorRequest request)
    {
        var result = await _proveedorService.UpdateAsync(id, request);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NOT_FOUND")
                return NotFound(new { message = result.ErrorMessage });
            if (result.HasFieldErrors)
                return BadRequest(new { message = result.ErrorMessage, errors = result.FieldErrors });
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(result.Value);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult> DeleteProveedor(int id)
    {
        var result = await _proveedorService.DeactivateAsync(id);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NOT_FOUND")
                return NotFound(new { message = result.ErrorMessage });
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(new { message = "Proveedor desactivado correctamente." });
    }
}
