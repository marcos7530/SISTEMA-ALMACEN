using System.Globalization;
using System.Text.Json;
using AfipSDK.Afip.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SistemaAlmacen.Business.Interfaces;
using SistemaAlmacen.Business.Models.Afip;

namespace SistemaAlmacen.Business.Services;

/// <summary>
/// Implementación del wrapper de AFIP/ARCA.
/// En modo desarrollo (Afip:UseMock=true) retorna respuestas mock exitosas con CAE generado.
/// En modo producción se conecta a los Web Services reales de ARCA a través del paquete Afip.Net (Afip SDK).
/// </summary>
public class AfipClientWrapper : IAfipClientWrapper
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AfipClientWrapper> _logger;
    private readonly bool _useMock;

    /// <summary>
    /// Tipos de comprobante que son notas de crédito (requieren comprobante asociado).
    /// </summary>
    private static readonly HashSet<int> TiposNotaCredito = new() { 3, 8, 13 };

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

    /// <summary>
    /// Construye la instancia del cliente Afip SDK a partir de la configuración.
    /// - Afip:Cuit, Afip:ModoProduccion, Afip:AccessToken (requerido).
    /// - Afip:Certificado y Afip:ClavePrivada: contenido PEM del certificado y clave privada.
    ///   Opcionales en desarrollo (CUIT de prueba 20409378472), requeridos en producción.
    /// </summary>
    private Afip BuildAfipClient()
    {
        var cuit = _configuration["Afip:Cuit"];
        var accessToken = _configuration["Afip:AccessToken"];
        var production = _configuration.GetValue<bool>("Afip:ModoProduccion", false);
        var cert = _configuration["Afip:Certificado"];
        var key = _configuration["Afip:ClavePrivada"];

        if (string.IsNullOrWhiteSpace(accessToken))
            throw new InvalidOperationException(
                "Falta configurar Afip:AccessToken. Obtené un access token gratuito en https://afipsdk.com y cargalo en la configuración (user-secrets o variables de entorno).");

        if (string.IsNullOrWhiteSpace(cuit))
            throw new InvalidOperationException("Falta configurar Afip:Cuit.");

        var options = new AfipOptions
        {
            CUIT = cuit,
            Production = production,
            AccessToken = accessToken
        };

        // El certificado y la clave son obligatorios en producción. En desarrollo con el CUIT
        // de prueba, Afip SDK permite operar sin ellos.
        if (!string.IsNullOrWhiteSpace(cert))
            options.Cert = cert;
        if (!string.IsNullOrWhiteSpace(key))
            options.Key = key;

        return new Afip(options);
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

        return await GetProductionVoucherInfoAsync(puntoDeVenta, tipoComprobante, numero);
    }

    /// <inheritdoc />
    public async Task<bool> TestConnectionAsync()
    {
        if (_useMock)
        {
            await Task.Delay(100); // Simular latencia
            return true;
        }

        try
        {
            var afip = BuildAfipClient();
            var status = await afip.ElectronicBilling.GetServerStatusAsync();

            // El WS devuelve AppServer/DbServer/AuthServer con valor "OK" cuando está operativo.
            var appOk = GetString(status, "AppServer").Equals("OK", StringComparison.OrdinalIgnoreCase);
            var dbOk = GetString(status, "DbServer").Equals("OK", StringComparison.OrdinalIgnoreCase);
            var authOk = GetString(status, "AuthServer").Equals("OK", StringComparison.OrdinalIgnoreCase);

            _logger.LogInformation("AFIP ServerStatus: App={App} Db={Db} Auth={Auth}", appOk, dbOk, authOk);
            return appOk && dbOk && authOk;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al verificar conectividad con AFIP.");
            return false;
        }
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
    /// Emisión real de comprobante vía los Web Services de ARCA (WSFEv1) usando Afip.Net.
    /// Obtiene el próximo número autorizado, arma la solicitud de CAE y devuelve el resultado.
    /// </summary>
    private async Task<AfipVoucherResponse> CreateProductionVoucherAsync(AfipVoucherRequest request)
    {
        Afip afip;
        try
        {
            afip = BuildAfipClient();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Configuración de AFIP incompleta.");
            return new AfipVoucherResponse
            {
                HasCae = false,
                ErrorMessage = ex.Message,
                Errors = new List<string> { "AFIP_CONFIG_INCOMPLETA" }
            };
        }

        // 1) Obtener el próximo número de comprobante autorizado para el PV y tipo.
        int proximoNumero;
        try
        {
            var ultimo = await afip.ElectronicBilling.GetLastVoucherAsync(request.PuntoDeVenta, request.TipoComprobante);
            proximoNumero = ultimo + 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener el último comprobante autorizado (PV {PV}, Tipo {Tipo}).",
                request.PuntoDeVenta, request.TipoComprobante);
            throw; // Lo maneja FacturacionService como pendiente por conexión.
        }

        // 2) Armar el payload del comprobante en el formato esperado por WSFEv1.
        var data = BuildVoucherData(request, proximoNumero);

        // 3) Solicitar el CAE (returnResponse=true para recibir la respuesta completa del WS).
        Dictionary<string, object> respuesta;
        try
        {
            respuesta = await afip.ElectronicBilling.CreateVoucherAsync(data, true);
        }
        catch (AfipWebServiceException ex)
        {
            // AFIP rechazó el comprobante (observaciones/errores de negocio): no es un error de conexión.
            _logger.LogWarning(ex, "AFIP rechazó el comprobante (PV {PV}, Tipo {Tipo}).",
                request.PuntoDeVenta, request.TipoComprobante);
            return new AfipVoucherResponse
            {
                HasCae = false,
                ErrorMessage = ex.Message,
                Errors = new List<string> { ex.Message }
            };
        }

        // 4) Mapear la respuesta a nuestro modelo.
        // Con returnResponse=true, el WSFEv1 devuelve la estructura anidada del SOAP:
        //   { FeCabResp: { Resultado }, FeDetResp: { FECAEDetResponse: [ { Resultado, CAE, CAEFchVto, ... } ] } }
        // por lo que el CAE NO está en el nivel raíz sino dentro de FECAEDetResponse.
        var rawJson = SerializeSafe(respuesta);
        var (cae, caeFchVto) = ExtractCae(rawJson);

        if (string.IsNullOrWhiteSpace(cae))
        {
            // Sin CAE: log de diagnóstico con la respuesta completa y extracción de observaciones/errores.
            _logger.LogWarning("AFIP no devolvió CAE. Respuesta completa: {Raw}", rawJson);

            var observaciones = ExtractObservaciones(rawJson);
            var detalle = observaciones.Count > 0
                ? string.Join(" | ", observaciones)
                : GetString(respuesta, "Observaciones", "Errors", "FchProceso");

            return new AfipVoucherResponse
            {
                HasCae = false,
                ErrorMessage = string.IsNullOrWhiteSpace(detalle) ? "AFIP no devolvió un CAE." : detalle,
                Errors = observaciones.Count > 0 ? observaciones : new List<string> { detalle }
            };
        }

        _logger.LogInformation(
            "AFIP: CAE obtenido {Cae} para comprobante PV {PV} Tipo {Tipo} Nro {Nro}",
            cae, request.PuntoDeVenta, request.TipoComprobante, proximoNumero);

        return new AfipVoucherResponse
        {
            HasCae = true,
            Cae = cae,
            CaeVencimiento = ParseAfipDate(caeFchVto),
            NumeroComprobante = proximoNumero,
            ErrorMessage = null,
            Errors = null
        };
    }

    /// <summary>
    /// Consulta real de un comprobante emitido en ARCA (WSFEv1) usando Afip.Net.
    /// </summary>
    private async Task<AfipVoucherInfo?> GetProductionVoucherInfoAsync(int puntoDeVenta, int tipoComprobante, long numero)
    {
        try
        {
            var afip = BuildAfipClient();
            var info = await afip.ElectronicBilling.GetVoucherInfoAsync((int)numero, puntoDeVenta, tipoComprobante);

            if (info is null || info.Count == 0)
                return null;

            return new AfipVoucherInfo
            {
                Numero = numero,
                Cae = GetString(info, "CodAutorizacion", "CAE"),
                CaeVencimiento = ParseAfipDate(GetString(info, "FchVto", "CAEFchVto")),
                TipoComprobante = tipoComprobante,
                Total = ParseDecimal(GetString(info, "ImpTotal"))
            };
        }
        catch (AfipWebServiceException ex)
        {
            _logger.LogWarning(ex, "AFIP: no se encontró el comprobante PV {PV} Tipo {Tipo} Nro {Nro}.",
                puntoDeVenta, tipoComprobante, numero);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al consultar comprobante en AFIP (PV {PV}, Tipo {Tipo}, Nro {Nro}).",
                puntoDeVenta, tipoComprobante, numero);
            return null;
        }
    }

    /// <summary>
    /// Construye el diccionario de datos del comprobante en el formato de WSFEv1 (FECAESolicitar).
    /// </summary>
    private Dictionary<string, object?> BuildVoucherData(AfipVoucherRequest request, int numero)
    {
        // Consumidor final por defecto (sin cliente identificado).
        // DocTipo 99 = Consumidor Final, DocNro 0.
        // CondicionIVAReceptorId 5 = Consumidor Final (requerido por AFIP desde 2024).
        var fecha = int.Parse(request.FechaComprobante.ToString("yyyyMMdd", CultureInfo.InvariantCulture));

        var data = new Dictionary<string, object?>
        {
            ["CantReg"] = 1,
            ["PtoVta"] = request.PuntoDeVenta,
            ["CbteTipo"] = request.TipoComprobante,
            ["Concepto"] = 1, // Productos
            ["DocTipo"] = 99, // Consumidor Final
            ["DocNro"] = 0,
            ["CbteDesde"] = numero,
            ["CbteHasta"] = numero,
            ["CbteFch"] = fecha,
            ["ImpTotal"] = request.Total,
            ["ImpTotConc"] = 0, // Neto no gravado
            ["ImpNeto"] = request.NetoGravado,
            ["ImpOpEx"] = request.Exento,
            ["ImpIVA"] = request.Iva,
            ["ImpTrib"] = 0,
            ["MonId"] = string.IsNullOrWhiteSpace(request.Moneda) ? "PES" : request.Moneda,
            ["MonCotiz"] = 1,
            ["CondicionIVAReceptorId"] = _configuration.GetValue<int>("Afip:CondicionIvaReceptorDefault", 5)
        };

        // Alícuota de IVA (21% => Id 5). Solo se envía para comprobantes A y B (no en C).
        // Factura C (11) y Nota de Crédito C (13) no discriminan IVA.
        var esComprobanteC = request.TipoComprobante is 11 or 13;
        if (!esComprobanteC && request.Iva > 0)
        {
            data["Iva"] = new[]
            {
                new Dictionary<string, object?>
                {
                    ["Id"] = 5, // 21%
                    ["BaseImp"] = request.NetoGravado,
                    ["Importe"] = request.Iva
                }
            };
        }

        // Para comprobante C, el "neto" va como importe total gravado sin IVA discriminado.
        if (esComprobanteC)
        {
            data["ImpNeto"] = request.Total;
            data["ImpIVA"] = 0;
        }

        // Comprobante asociado (notas de crédito).
        if (TiposNotaCredito.Contains(request.TipoComprobante)
            && request.NumeroComprobanteAsociado.HasValue
            && request.TipoComprobanteAsociado.HasValue
            && request.PuntoDeVentaAsociado.HasValue)
        {
            data["CbtesAsoc"] = new[]
            {
                new Dictionary<string, object?>
                {
                    ["Tipo"] = request.TipoComprobanteAsociado.Value,
                    ["PtoVta"] = request.PuntoDeVentaAsociado.Value,
                    ["Nro"] = request.NumeroComprobanteAsociado.Value
                }
            };
        }

        return data;
    }

    // --- Helpers de parsing de respuestas del SDK (Dictionary<string, object>) ---

    /// <summary>
    /// Serializa la respuesta del SDK a JSON para diagnóstico, sin lanzar excepciones.
    /// </summary>
    private static string SerializeSafe(object? value)
    {
        try
        {
            return JsonSerializer.Serialize(value);
        }
        catch (Exception ex)
        {
            return $"(no serializable: {ex.Message})";
        }
    }

    /// <summary>
    /// Extrae el CAE y su fecha de vencimiento de la respuesta cruda del WSFEv1.
    /// El CAE puede venir en el nivel raíz (cuando el SDK ya lo aplana) o anidado en
    /// FeDetResp → FECAEDetResponse[0] → { CAE, CAEFchVto }. Busca recursivamente el primer
    /// par CAE/CAEFchVto no vacío para cubrir ambas formas.
    /// </summary>
    private static (string cae, string caeFchVto) ExtractCae(string rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
            return (string.Empty, string.Empty);

        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            return FindCae(doc.RootElement);
        }
        catch (JsonException)
        {
            return (string.Empty, string.Empty);
        }
    }

    /// <summary>
    /// Recorre recursivamente el JSON buscando el primer objeto que tenga una propiedad "CAE"
    /// con valor no vacío, y devuelve su CAE junto al CAEFchVto del mismo objeto (si existe).
    /// </summary>
    private static (string cae, string caeFchVto) FindCae(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                if (element.TryGetProperty("CAE", out var caeProp))
                {
                    var cae = JsonValueToString(caeProp);
                    if (!string.IsNullOrWhiteSpace(cae))
                    {
                        var fchVto = element.TryGetProperty("CAEFchVto", out var vtoProp)
                            ? JsonValueToString(vtoProp)
                            : string.Empty;
                        return (cae, fchVto);
                    }
                }

                foreach (var prop in element.EnumerateObject())
                {
                    var result = FindCae(prop.Value);
                    if (!string.IsNullOrWhiteSpace(result.cae))
                        return result;
                }
                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    var result = FindCae(item);
                    if (!string.IsNullOrWhiteSpace(result.cae))
                        return result;
                }
                break;
        }

        return (string.Empty, string.Empty);
    }

    /// <summary>
    /// Convierte un valor JSON (string o número) a su representación de texto.
    /// </summary>
    private static string JsonValueToString(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.Number => value.ToString(),
            _ => string.Empty
        };
    }

    /// <summary>
    /// Recorre el JSON crudo de la respuesta de AFIP y extrae los mensajes de observaciones y
    /// errores. El WSFEv1 los devuelve anidados (FeDetResp → Observaciones → Obs → {Code, Msg},
    /// y también Errors → Err → {Code, Msg}). Busca recursivamente cualquier propiedad "Msg".
    /// </summary>
    private static List<string> ExtractObservaciones(string rawJson)
    {
        var mensajes = new List<string>();
        if (string.IsNullOrWhiteSpace(rawJson))
            return mensajes;

        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            CollectMsg(doc.RootElement, mensajes);
        }
        catch (JsonException)
        {
            // Si no es JSON válido, no hay nada que extraer.
        }

        return mensajes;
    }

    /// <summary>
    /// Recorre recursivamente un elemento JSON acumulando el texto de toda propiedad "Msg"
    /// (case-insensitive), tal como las usa AFIP para observaciones y errores.
    /// </summary>
    private static void CollectMsg(JsonElement element, List<string> mensajes)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                {
                    if (string.Equals(prop.Name, "Msg", StringComparison.OrdinalIgnoreCase)
                        && prop.Value.ValueKind == JsonValueKind.String)
                    {
                        var msg = prop.Value.GetString();
                        if (!string.IsNullOrWhiteSpace(msg))
                            mensajes.Add(msg!.Trim());
                    }
                    else
                    {
                        CollectMsg(prop.Value, mensajes);
                    }
                }
                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                    CollectMsg(item, mensajes);
                break;
        }
    }

    /// <summary>
    /// Obtiene el primer valor no vacío entre las claves dadas del diccionario de respuesta.
    /// </summary>
    private static string GetString(IReadOnlyDictionary<string, object> dict, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (dict.TryGetValue(key, out var value) && value is not null)
            {
                var s = value.ToString();
                if (!string.IsNullOrWhiteSpace(s))
                    return s!;
            }
        }
        return string.Empty;
    }

    /// <summary>
    /// Parsea una fecha de AFIP. Acepta formatos yyyyMMdd y yyyy-MM-dd.
    /// </summary>
    private static DateTime ParseAfipDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return DateTime.MinValue;

        var formats = new[] { "yyyyMMdd", "yyyy-MM-dd" };
        if (DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
            return fecha;

        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var generico)
            ? generico
            : DateTime.MinValue;
    }

    /// <summary>
    /// Parsea un decimal en formato invariante (punto como separador decimal).
    /// </summary>
    private static decimal ParseDecimal(string value)
    {
        return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0m;
    }
}
