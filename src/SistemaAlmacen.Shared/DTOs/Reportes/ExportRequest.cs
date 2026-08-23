using System.ComponentModel.DataAnnotations;
using SistemaAlmacen.Shared.Enums;

namespace SistemaAlmacen.Shared.DTOs.Reportes;

/// <summary>
/// Request para exportar un reporte.
/// </summary>
public class ExportRequest
{
    [Required(ErrorMessage = "El formato es requerido.")]
    public FormatoExportacion Formato { get; set; }

    [Required(ErrorMessage = "El tipo de reporte es requerido.")]
    [MaxLength(50, ErrorMessage = "El tipo de reporte no puede exceder 50 caracteres.")]
    public string TipoReporte { get; set; } = string.Empty;

    [Required(ErrorMessage = "La fecha de inicio es requerida.")]
    public DateTime FechaInicio { get; set; }

    [Required(ErrorMessage = "La fecha de fin es requerida.")]
    public DateTime FechaFin { get; set; }
}
