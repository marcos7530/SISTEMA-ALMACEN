namespace SistemaAlmacen.Shared.Exceptions;

/// <summary>
/// Excepción lanzada cuando los datos proporcionados no pasan la validación de negocio.
/// </summary>
public class ValidationException : Exception
{
    /// <summary>
    /// Errores de validación por campo (clave: nombre del campo, valor: mensajes de error).
    /// </summary>
    public Dictionary<string, string[]>? FieldErrors { get; }

    public ValidationException(string message)
        : base(message)
    {
    }

    public ValidationException(string message, Dictionary<string, string[]> fieldErrors)
        : base(message)
    {
        FieldErrors = fieldErrors;
    }

    public ValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
