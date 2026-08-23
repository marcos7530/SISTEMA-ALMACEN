using SistemaAlmacen.Shared.Enums;

namespace SistemaAlmacen.Shared.DTOs.Auditoria;

/// <summary>
/// Filtro para listado paginado de registros de auditoría.
/// </summary>
public class AuditoriaFilter
{
    public int? UsuarioId { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public TipoOperacion? TipoOperacion { get; set; }
    public string? EntidadAfectada { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
