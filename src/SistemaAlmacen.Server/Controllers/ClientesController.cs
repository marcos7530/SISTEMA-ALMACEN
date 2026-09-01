using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaAlmacen.Business.Interfaces;
using SistemaAlmacen.Shared.DTOs;
using SistemaAlmacen.Shared.DTOs.Clientes;

namespace SistemaAlmacen.Server.Controllers;

/// <summary>
/// Controller para la gestión de clientes.
/// Lectura disponible para Vendedor y Administrador (para asociar clientes en ventas),
/// escritura restringida a Administrador.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClientesController : ControllerBase
{
    private readonly IClienteService _clienteService;

    public ClientesController(IClienteService clienteService)
    {
        _clienteService = clienteService;
    }

    /// <summary>
    /// Obtiene un listado paginado de clientes activos con búsqueda.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "RequireVendedorOrAdmin")]
    public async Task<ActionResult<PaginatedResult<ClienteDto>>> GetClientes(
        [FromQuery] string? searchTerm,
        [FromQuery] bool soloCuentaCorriente = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var filter = new ClienteFilter
        {
            SearchTerm = searchTerm,
            SoloCuentaCorriente = soloCuentaCorriente,
            Page = page,
            PageSize = pageSize
        };

        var result = await _clienteService.GetClientesAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Obtiene un cliente por su ID.
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Policy = "RequireVendedorOrAdmin")]
    public async Task<ActionResult<ClienteDto>> GetCliente(int id)
    {
        var cliente = await _clienteService.GetByIdAsync(id);

        if (cliente is null)
            return NotFound(new { message = "Cliente no encontrado." });

        return Ok(cliente);
    }

    /// <summary>
    /// Crea un nuevo cliente. Solo Administrador.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<ClienteDto>> CreateCliente([FromBody] CreateClienteRequest request)
    {
        var result = await _clienteService.CreateAsync(request);

        if (!result.IsSuccess)
        {
            if (result.HasFieldErrors)
                return BadRequest(new { message = result.ErrorMessage, errors = result.FieldErrors });
            return BadRequest(new { message = result.ErrorMessage });
        }

        return CreatedAtAction(nameof(GetCliente), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>
    /// Actualiza un cliente existente. Solo Administrador.
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<ClienteDto>> UpdateCliente(int id, [FromBody] UpdateClienteRequest request)
    {
        var result = await _clienteService.UpdateAsync(id, request);

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

    /// <summary>
    /// Desactiva (eliminación lógica) un cliente. Solo Administrador.
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult> DeleteCliente(int id)
    {
        var result = await _clienteService.DeactivateAsync(id);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NOT_FOUND")
                return NotFound(new { message = result.ErrorMessage });
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(new { message = "Cliente desactivado correctamente." });
    }
}
