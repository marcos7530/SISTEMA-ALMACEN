namespace SistemaAlmacen.Shared.DTOs.Usuarios;

/// <summary>
/// Filtro para listado paginado de usuarios.
/// </summary>
public class UsuarioFilter
{
    public string? SearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
