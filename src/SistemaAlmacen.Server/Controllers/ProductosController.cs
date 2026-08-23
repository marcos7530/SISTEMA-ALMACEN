using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaAlmacen.Business.Interfaces;
using SistemaAlmacen.Shared.DTOs;
using SistemaAlmacen.Shared.DTOs.Productos;

namespace SistemaAlmacen.Server.Controllers;

/// <summary>
/// Controller para la gestión de productos.
/// Lectura disponible para todos los roles autenticados, escritura restringida a Administrador.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductosController : ControllerBase
{
    private readonly IProductoService _productoService;

    public ProductosController(IProductoService productoService)
    {
        _productoService = productoService;
    }

    /// <summary>
    /// Obtiene un listado paginado de productos activos con búsqueda y filtro por categoría.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "RequireVendedorOrAdmin")]
    public async Task<ActionResult<PaginatedResult<ProductoDto>>> GetProductos(
        [FromQuery] string? searchTerm,
        [FromQuery] int? categoriaId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var filter = new ProductoFilter
        {
            SearchTerm = searchTerm,
            CategoriaId = categoriaId,
            Page = page,
            PageSize = pageSize
        };

        var result = await _productoService.GetProductosAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Obtiene un producto por su ID.
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Policy = "RequireVendedorOrAdmin")]
    public async Task<ActionResult<ProductoDto>> GetProducto(int id)
    {
        var producto = await _productoService.GetByIdAsync(id);

        if (producto is null)
        {
            return NotFound(new { message = "Producto no encontrado." });
        }

        return Ok(producto);
    }

    /// <summary>
    /// Crea un nuevo producto. Solo Administrador.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<ProductoDto>> CreateProducto([FromBody] CreateProductoRequest request)
    {
        var result = await _productoService.CreateAsync(request);

        if (!result.IsSuccess)
        {
            if (result.HasFieldErrors)
            {
                return BadRequest(new { message = result.ErrorMessage, errors = result.FieldErrors });
            }
            return BadRequest(new { message = result.ErrorMessage });
        }

        return CreatedAtAction(nameof(GetProducto), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>
    /// Actualiza un producto existente. Solo Administrador.
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<ProductoDto>> UpdateProducto(int id, [FromBody] UpdateProductoRequest request)
    {
        var result = await _productoService.UpdateAsync(id, request);

        if (!result.IsSuccess)
        {
            if (result.HasFieldErrors)
            {
                return BadRequest(new { message = result.ErrorMessage, errors = result.FieldErrors });
            }
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Desactiva (eliminación lógica) un producto. Solo Administrador.
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult> DeleteProducto(int id)
    {
        var result = await _productoService.DeactivateAsync(id);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(new { message = "Producto desactivado correctamente." });
    }
}
