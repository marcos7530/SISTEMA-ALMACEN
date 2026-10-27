using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaAlmacen.Data.Context;

namespace SistemaAlmacen.Server.Controllers;

/// <summary>
/// Controller para operaciones de datos de demostración.
/// Acceso restringido a Administrador y gateado por configuración de entorno.
/// </summary>
[ApiController]
[Route("api/demo")]
[Authorize(Policy = "RequireAdmin")]
public class DemoController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DemoController> _logger;
    private readonly IConfiguration _configuration;

    public DemoController(
        ApplicationDbContext context,
        ILogger<DemoController> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Borra las ventas de demostración (de mostrador) del mes en curso y las vuelve a generar.
    /// Solo opera si la bandera <c>Demo:RegeneracionHabilitada</c> está en <c>true</c>
    /// (deshabilitada por defecto, p.ej. en producción).
    /// </summary>
    [HttpPost("regenerar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RegenerarDemo()
    {
        var habilitado = _configuration.GetValue<bool>("Demo:RegeneracionHabilitada", false);
        if (!habilitado)
        {
            _logger.LogWarning(
                "Intento de regeneración de datos demo con la función deshabilitada por configuración.");
            return StatusCode(StatusCodes.Status403Forbidden,
                new { mensaje = "La regeneración de datos demo está deshabilitada en este entorno." });
        }

        var ventasCreadas = await DataSeeder.RegenerarDemoVentasAsync(_context, _logger);

        return Ok(new
        {
            ventasCreadas,
            mensaje = $"Se regeneraron los datos demo del mes: {ventasCreadas} venta(s) creada(s)."
        });
    }
}
