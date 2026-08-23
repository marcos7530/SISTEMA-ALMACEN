namespace SistemaAlmacen.Shared.Exceptions;

/// <summary>
/// Excepción lanzada cuando el usuario no tiene permisos para realizar la operación solicitada.
/// </summary>
public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message)
        : base(message)
    {
    }

    public UnauthorizedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
