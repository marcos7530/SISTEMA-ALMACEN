namespace SistemaAlmacen.Shared.Common;

/// <summary>
/// Representa el resultado de una operación que puede ser exitosa o fallida.
/// Soporta mensajes de error simples, errores por campo y códigos de error programáticos.
/// </summary>
public class Result
{
    /// <summary>
    /// Indica si la operación fue exitosa.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Mensaje de error cuando la operación falla. Null si fue exitosa.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Código de error para identificación programática. Null si fue exitosa o no aplica.
    /// </summary>
    public string? ErrorCode { get; }

    /// <summary>
    /// Errores de validación por campo. Null si no hay errores de validación por campo.
    /// </summary>
    public Dictionary<string, string[]>? FieldErrors { get; }

    /// <summary>
    /// Indica si el resultado contiene errores de validación por campo.
    /// </summary>
    public bool HasFieldErrors => FieldErrors is not null && FieldErrors.Count > 0;

    protected Result(bool isSuccess, string? errorMessage = null, string? errorCode = null, Dictionary<string, string[]>? fieldErrors = null)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        ErrorCode = errorCode;
        FieldErrors = fieldErrors;
    }

    /// <summary>
    /// Crea un resultado exitoso.
    /// </summary>
    public static Result Success() => new(true);

    /// <summary>
    /// Crea un resultado fallido con un mensaje de error y opcionalmente un código de error.
    /// </summary>
    /// <param name="message">Mensaje descriptivo del error.</param>
    /// <param name="code">Código de error para identificación programática (opcional).</param>
    public static Result Failure(string message, string? code = null) => new(false, message, code);

    /// <summary>
    /// Crea un resultado fallido con errores de validación por campo.
    /// </summary>
    /// <param name="errors">Diccionario de errores donde la clave es el nombre del campo y el valor es un arreglo de mensajes de error.</param>
    public static Result ValidationFailure(Dictionary<string, string[]> errors) =>
        new(false, "Error de validación.", null, errors);
}

/// <summary>
/// Representa el resultado de una operación que puede ser exitosa con un valor o fallida.
/// Hereda de Result y agrega un valor tipado.
/// </summary>
/// <typeparam name="T">Tipo del valor contenido en caso de éxito.</typeparam>
public class Result<T> : Result
{
    /// <summary>
    /// Valor resultante de la operación exitosa. Default si la operación falló.
    /// </summary>
    public T? Value { get; }

    private Result(T value) : base(true)
    {
        Value = value;
    }

    private Result(string errorMessage, string? errorCode) : base(false, errorMessage, errorCode)
    {
        Value = default;
    }

    private Result(Dictionary<string, string[]> fieldErrors) : base(false, "Error de validación.", null, fieldErrors)
    {
        Value = default;
    }

    /// <summary>
    /// Crea un resultado exitoso con un valor.
    /// </summary>
    /// <param name="value">Valor resultante de la operación.</param>
    public static Result<T> Success(T value) => new(value);

    /// <summary>
    /// Crea un resultado fallido con un mensaje de error y opcionalmente un código de error.
    /// </summary>
    /// <param name="message">Mensaje descriptivo del error.</param>
    /// <param name="code">Código de error para identificación programática (opcional).</param>
    public new static Result<T> Failure(string message, string? code = null) => new(message, code);

    /// <summary>
    /// Crea un resultado fallido con errores de validación por campo.
    /// </summary>
    /// <param name="errors">Diccionario de errores donde la clave es el nombre del campo y el valor es un arreglo de mensajes de error.</param>
    public new static Result<T> ValidationFailure(Dictionary<string, string[]> errors) => new(errors);

    /// <summary>
    /// Conversión implícita de un valor T a Result&lt;T&gt; exitoso.
    /// Permite retornar directamente un valor donde se espera un Result&lt;T&gt;.
    /// </summary>
    public static implicit operator Result<T>(T value) => Success(value);
}
