using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SistemaAlmacen.Shared.DTOs;
using SistemaAlmacen.Shared.Exceptions;

namespace SistemaAlmacen.Server.Middleware;

/// <summary>
/// Middleware global de manejo de excepciones.
/// Intercepta todas las excepciones no controladas y retorna respuestas JSON seguras
/// sin exponer stack traces, cadenas de conexión ni sentencias SQL.
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = context.TraceIdentifier;

        var (statusCode, message, fieldErrors) = exception switch
        {
            ValidationException validationEx => (
                (int)HttpStatusCode.BadRequest,
                validationEx.Message,
                validationEx.FieldErrors
            ),
            UnauthorizedException unauthorizedEx => (
                (int)HttpStatusCode.Forbidden,
                unauthorizedEx.Message,
                (Dictionary<string, string[]>?)null
            ),
            DbUpdateException dbEx => (
                (int)HttpStatusCode.InternalServerError,
                "Ocurrió un error al procesar la operación en la base de datos. Intente nuevamente.",
                (Dictionary<string, string[]>?)null
            ),
            _ => (
                (int)HttpStatusCode.InternalServerError,
                "Ocurrió un error interno en el servidor. Intente nuevamente más tarde.",
                (Dictionary<string, string[]>?)null
            )
        };

        // Log completo de la excepción internamente (incluye stack trace, inner exceptions, etc.)
        _logger.LogError(exception,
            "Excepción no controlada | TraceId: {TraceId} | StatusCode: {StatusCode} | Tipo: {ExceptionType} | Mensaje: {Message}",
            traceId, statusCode, exception.GetType().Name, exception.Message);

        var errorResponse = new ErrorResponse
        {
            Message = message,
            StatusCode = statusCode,
            TraceId = traceId,
            FieldErrors = fieldErrors
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        var json = JsonSerializer.Serialize(errorResponse, jsonOptions);
        await context.Response.WriteAsync(json);
    }
}
