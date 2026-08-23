using SistemaAlmacen.Shared.Enums;

namespace SistemaAlmacen.Shared.DTOs.Auditoria;

/// <summary>
/// DTO de respuesta con datos de un registro de auditoría.
/// </summary>
public class AuditoriaDto
{
    public long Id { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public TipoOperacion TipoOperacion { get; set; }
    public string EntidadAfectada { get; set; } = string.Empty;
    public string RegistroAfectadoId { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
}
