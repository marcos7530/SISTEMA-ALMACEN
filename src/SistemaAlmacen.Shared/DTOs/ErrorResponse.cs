using System.Text.Json.Serialization;

namespace SistemaAlmacen.Shared.DTOs;

/// <summary>
/// Respuesta de error estándar retornada por la API cuando ocurre una excepción.
/// No expone información sensible como stack traces, cadenas de conexión o sentencias SQL.
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Mensaje seguro para el usuario describiendo el error.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Código de estado HTTP asociado al error.
    /// </summary>
    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; }

    /// <summary>
    /// Identificador único de la solicitud para correlación con logs internos.
    /// </summary>
    [JsonPropertyName("traceId")]
    public string? TraceId { get; set; }

    /// <summary>
    /// Errores de validación por campo (solo presente en errores 400).
    /// </summary>
    [JsonPropertyName("fieldErrors")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string[]>? FieldErrors { get; set; }
}
