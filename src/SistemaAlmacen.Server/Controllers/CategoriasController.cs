using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaAlmacen.Business.Interfaces;
using SistemaAlmacen.Shared.DTOs.Categorias;

namespace SistemaAlmacen.Server.Controllers;

/// <summary>
/// Controller para gestión de categorías de productos.
/// Lectura disponible para Vendedor y Administrador; escritura restringida a Administrador.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CategoriasController : ControllerBase
{
    private readonly ICategoriaService _categoriaService;

    public CategoriasController(ICategoriaService categoriaService)
    {
        _categoriaService = categoriaService;
    }

    /// <summary>
    /// Obtiene todas las categorías ordenadas alfabéticamente.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "RequireVendedorOrAdmin")]
    public async Task<ActionResult<List<CategoriaDto>>> GetAll()
    {
        var categorias = await _categoriaService.GetAllAsync();
        return Ok(categorias);
    }

    /// <summary>
    /// Crea una nueva categoría. Solo Administrador.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<CategoriaDto>> Create([FromBody] CreateCategoriaRequest request)
    {
        var result = await _categoriaService.CreateAsync(request);

        if (!result.IsSuccess)
        {
            if (result.HasFieldErrors)
                return BadRequest(new { errors = result.FieldErrors });

            return BadRequest(new { message = result.ErrorMessage });
        }

        return CreatedAtAction(nameof(GetAll), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>
    /// Actualiza una categoría existente. Solo Administrador.
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<CategoriaDto>> Update(int id, [FromBody] UpdateCategoriaRequest request)
    {
        var result = await _categoriaService.UpdateAsync(id, request);

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
    /// Elimina una categoría (solo si no tiene productos asociados). Solo Administrador.
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult> Delete(int id)
    {
        var result = await _categoriaService.DeleteAsync(id);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NOT_FOUND")
                return NotFound(new { message = result.ErrorMessage });

            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(new { message = "Categoría eliminada correctamente." });
    }
}
