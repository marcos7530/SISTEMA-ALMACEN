using System.Security.Claims;
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
    private readonly IStockService _stockService;

    public ProductosController(IProductoService productoService, IStockService stockService)
    {
        _productoService = productoService;
        _stockService = stockService;
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
    /// Obtiene un producto por su código de barras.
    /// </summary>
    [HttpGet("barcode/{codigo}")]
    [Authorize(Policy = "RequireVendedorOrAdmin")]
    public async Task<ActionResult<ProductoDto>> GetProductoByBarcode(string codigo)
    {
        var producto = await _productoService.GetByCodigoBarrasAsync(codigo);

        if (producto is null)
        {
            return NotFound(new { message = "Producto no encontrado con ese código de barras." });
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

    /// <summary>
    /// Da de baja stock de un producto por un motivo (rotura, vencimiento, etc.). Solo Administrador.
    /// </summary>
    [HttpPost("{id}/baja-stock")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<ProductoDto>> BajaStock(int id, [FromBody] AjusteBajaStockRequest request)
    {
        var usuarioId = GetCurrentUserId();
        if (usuarioId is null)
        {
            return Unauthorized(new { message = "No se pudo identificar al usuario." });
        }

        var result = await _stockService.RegistrarBajaAsync(id, request, usuarioId.Value);

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
    /// Repone (incrementa) stock de un producto identificado por su código de barras. Solo Administrador.
    /// </summary>
    [HttpPost("reponer-stock")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<ProductoDto>> ReponerStock([FromBody] ReponerStockRequest request)
    {
        var usuarioId = GetCurrentUserId();
        if (usuarioId is null)
        {
            return Unauthorized(new { message = "No se pudo identificar al usuario." });
        }

        var result = await _stockService.IncrementarPorCodigoBarrasAsync(request, usuarioId.Value);

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
    /// Extrae el ID del usuario actual desde los claims del JWT.
    /// </summary>
    private int? GetCurrentUserId()
    {
        var claim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)
                    ?? HttpContext.User.FindFirst("sub");

        if (claim is null || !int.TryParse(claim.Value, out var userId))
        {
            return null;
        }

        return userId;
    }
}
