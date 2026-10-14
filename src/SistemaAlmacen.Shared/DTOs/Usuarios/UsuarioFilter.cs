namespace SistemaAlmacen.Shared.DTOs.Usuarios;

/// <summary>
/// Filtro para listado paginado de usuarios.
/// </summary>
public class UsuarioFilter
{
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Filtra por estado del usuario: null = todos, true = solo activos, false = solo inactivos.
    /// </summary>
    public bool? Activo { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
