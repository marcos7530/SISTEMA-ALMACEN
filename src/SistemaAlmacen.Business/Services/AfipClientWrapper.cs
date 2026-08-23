using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SistemaAlmacen.Business.Interfaces;
using SistemaAlmacen.Business.Models.Afip;

namespace SistemaAlmacen.Business.Services;

/// <summary>
/// Implementación del wrapper de AFIP/ARCA.
/// En modo desarrollo retorna respuestas mock exitosas con CAE generado.
/// En modo producción se conectará a los Web Services reales de AFIP.
/// </summary>
public class AfipClientWrapper : IAfipClientWrapper
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AfipClientWrapper> _logger;
    private readonly bool _useMock;

    /// <summary>
    /// Contador interno para generar números de comprobante secuenciales en modo mock.
    /// </summary>
    private static long _mockComprobanteCounter = 1000;

    public AfipClientWrapper(IConfiguration configuration, ILogger<AfipClientWrapper> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _useMock = _configuration.GetValue<bool>("Afip:UseMock", true);
    }

    /// <inheritdoc />
    public async Task<AfipVoucherResponse> CreateNextVoucherAsync(AfipVoucherRequest request)
    {
        if (_useMock)
        {
            return await CreateMockVoucherAsync(request);
        }

        return await CreateProductionVoucherAsync(request);
    }

    /// <inheritdoc />
    public async Task<AfipVoucherInfo?> GetVoucherInfoAsync(int puntoDeVenta, int tipoComprobante, long numero)
    {
        if (_useMock)
        {
            return await GetMockVoucherInfoAsync(puntoDeVenta, tipoComprobante, numero);
        }

        // Producción: conectar a AFIP WS para consultar comprobante
        _logger.LogWarning("GetVoucherInfoAsync en modo producción no implementado aún.");
        return null;
    }

    /// <inheritdoc />
    public async Task<bool> TestConnectionAsync()
    {
        if (_useMock)
        {
            await Task.Delay(100); // Simular latencia
            return true;
        }

        // Producción: probar conectividad con AFIP
        _logger.LogWarning("TestConnectionAsync en modo producción no implementado aún.");
        return false;
    }

    /// <summary>
    /// Genera una respuesta mock exitosa simulando la emisión de CAE por AFIP.
    /// </summary>
    private Task<AfipVoucherResponse> CreateMockVoucherAsync(AfipVoucherRequest request)
    {
        var numeroComprobante = Interlocked.Increment(ref _mockComprobanteCounter);
        var cae = GenerateMockCae();
        var vencimiento = request.FechaComprobante.AddDays(10);

        _logger.LogInformation(
            "AFIP Mock: Emitido comprobante tipo {Tipo} PV {PV} nro {Nro} CAE {Cae}",
            request.TipoComprobante, request.PuntoDeVenta, numeroComprobante, cae);

        var response = new AfipVoucherResponse
        {
            HasCae = true,
            Cae = cae,
            CaeVencimiento = vencimiento,
            NumeroComprobante = numeroComprobante,
            ErrorMessage = null,
            Errors = null
        };

        return Task.FromResult(response);
    }

    /// <summary>
    /// Consulta mock de un comprobante previamente emitido.
    /// </summary>
    private Task<AfipVoucherInfo?> GetMockVoucherInfoAsync(int puntoDeVenta, int tipoComprobante, long numero)
    {
        var info = new AfipVoucherInfo
        {
            Numero = numero,
            Cae = GenerateMockCae(),
            CaeVencimiento = DateTime.UtcNow.AddDays(10),
            TipoComprobante = tipoComprobante,
            Total = 0 // No tenemos el monto original en modo mock
        };

        return Task.FromResult<AfipVoucherInfo?>(info);
    }

    /// <summary>
    /// Genera un CAE mock de 14 dígitos numéricos.
    /// </summary>
    private static string GenerateMockCae()
    {
        var random = new Random();
        var cae = string.Empty;
        for (int i = 0; i < 14; i++)
        {
            cae += random.Next(0, 10).ToString();
        }
        return cae;
    }

    /// <summary>
    /// Emisión real de comprobante vía Web Services AFIP (no implementado aún).
    /// </summary>
    private Task<AfipVoucherResponse> CreateProductionVoucherAsync(AfipVoucherRequest request)
    {
        // TODO: Implementar conexión real con Web Services AFIP/ARCA
        // Requiere certificado digital, CUIT configurado y endpoint WSDL
        _logger.LogError("CreateProductionVoucherAsync: Modo producción no implementado. Configure Afip:UseMock=true.");

        var response = new AfipVoucherResponse
        {
            HasCae = false,
            ErrorMessage = "Modo producción de AFIP no implementado. Configure el sistema en modo mock para desarrollo.",
            Errors = new List<string> { "AFIP_NOT_CONFIGURED" }
        };

        return Task.FromResult(response);
    }
}
