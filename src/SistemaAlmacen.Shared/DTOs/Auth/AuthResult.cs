namespace SistemaAlmacen.Shared.DTOs.Auth;

/// <summary>
/// Resultado de una operación de autenticación.
/// </summary>
public class AuthResult
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Rol { get; set; }
}
