using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Forms;

namespace SistemaAlmacen.Client.Services;

/// <summary>
/// Resultado del parseo de errores de una respuesta API.
/// </summary>
public sealed class ApiErrorResult
{
    /// <summary>
    /// Mensaje de error general (no asociado a un campo específico).
    /// </summary>
    public string? GeneralError { get; init; }

    /// <summary>
    /// Errores de validación por campo. La clave es el nombre del campo,
    /// el valor es la lista de mensajes de error para ese campo.
    /// </summary>
    public Dictionary<string, string[]> FieldErrors { get; init; } = new();

    /// <summary>
    /// Indica si hay errores de campo.
    /// </summary>
    public bool HasFieldErrors => FieldErrors.Count > 0;

    /// <summary>
    /// Indica si hay algún error (general o de campo).
    /// </summary>
    public bool HasErrors => !string.IsNullOrWhiteSpace(GeneralError) || HasFieldErrors;
}

/// <summary>
/// Helper para parsear respuestas de error de la API y mapearlas a mensajes de EditContext.
/// Soporta los formatos de error del backend:
/// - { "errors": { "campo": ["mensaje1", "mensaje2"] } } para errores de validación por campo
/// - { "message": "texto" } para errores generales
/// </summary>
public static class ApiErrorParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Parsea la respuesta HTTP de error y extrae los errores estructurados.
    /// </summary>
    /// <param name="response">Respuesta HTTP con código de error.</param>
    /// <returns>Resultado con errores parseados.</returns>
    public static async Task<ApiErrorResult> ParseAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return new ApiErrorResult();
        }

        try
        {
            var content = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(content))
            {
                return new ApiErrorResult
                {
                    GeneralError = GetDefaultErrorMessage(response.StatusCode)
                };
            }

            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            // Intenta extraer errores de campo: { "errors": { "field": ["msg"] } }
            if (root.TryGetProperty("errors", out var errorsElement) &&
                errorsElement.ValueKind == JsonValueKind.Object)
            {
                var fieldErrors = new Dictionary<string, string[]>();
                foreach (var property in errorsElement.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.Array)
                    {
                        var messages = property.Value.EnumerateArray()
                            .Select(e => e.GetString() ?? string.Empty)
                            .Where(s => !string.IsNullOrWhiteSpace(s))
                            .ToArray();

                        if (messages.Length > 0)
                        {
                            fieldErrors[property.Name] = messages;
                        }
                    }
                }

                if (fieldErrors.Count > 0)
                {
                    return new ApiErrorResult { FieldErrors = fieldErrors };
                }
            }

            // Intenta extraer mensaje general: { "message": "texto" }
            if (root.TryGetProperty("message", out var messageElement) &&
                messageElement.ValueKind == JsonValueKind.String)
            {
                return new ApiErrorResult
                {
                    GeneralError = messageElement.GetString()
                };
            }

            // Intenta el formato de ProblemDetails: { "title": "texto", "detail": "detalle" }
            if (root.TryGetProperty("title", out var titleElement) &&
                titleElement.ValueKind == JsonValueKind.String)
            {
                var detail = root.TryGetProperty("detail", out var detailElement)
                    ? detailElement.GetString()
                    : null;

                return new ApiErrorResult
                {
                    GeneralError = detail ?? titleElement.GetString()
                };
            }

            return new ApiErrorResult
            {
                GeneralError = GetDefaultErrorMessage(response.StatusCode)
            };
        }
        catch (JsonException)
        {
            return new ApiErrorResult
            {
                GeneralError = GetDefaultErrorMessage(response.StatusCode)
            };
        }
    }

    /// <summary>
    /// Parsea la respuesta de error y mapea los errores de campo al EditContext proporcionado.
    /// Esto permite que los campos del formulario se resalten con mensajes de error específicos.
    /// </summary>
    /// <param name="response">Respuesta HTTP con código de error.</param>
    /// <param name="editContext">EditContext del formulario donde mapear los errores.</param>
    /// <returns>Resultado con errores parseados (también mapeados al EditContext).</returns>
    public static async Task<ApiErrorResult> ParseAndMapToEditContextAsync(
        HttpResponseMessage response,
        EditContext editContext)
    {
        var result = await ParseAsync(response);

        if (result.HasFieldErrors)
        {
            var messageStore = new ValidationMessageStore(editContext);
            messageStore.Clear();

            foreach (var fieldError in result.FieldErrors)
            {
                var fieldIdentifier = new FieldIdentifier(editContext.Model, fieldError.Key);
                foreach (var message in fieldError.Value)
                {
                    messageStore.Add(fieldIdentifier, message);
                }
            }

            editContext.NotifyValidationStateChanged();
        }

        return result;
    }

    private static string GetDefaultErrorMessage(System.Net.HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            System.Net.HttpStatusCode.BadRequest => "Los datos enviados no son válidos.",
            System.Net.HttpStatusCode.Unauthorized => "No tiene autorización. Inicie sesión nuevamente.",
            System.Net.HttpStatusCode.Forbidden => "No tiene permisos para realizar esta acción.",
            System.Net.HttpStatusCode.NotFound => "El recurso solicitado no fue encontrado.",
            System.Net.HttpStatusCode.Conflict => "La operación genera un conflicto con datos existentes.",
            System.Net.HttpStatusCode.RequestTimeout => "La operación tardó demasiado. Intente nuevamente.",
            System.Net.HttpStatusCode.InternalServerError => "Error interno del servidor. Intente nuevamente.",
            _ => "Ocurrió un error inesperado. Intente nuevamente."
        };
    }
}
