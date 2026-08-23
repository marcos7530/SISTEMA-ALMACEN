using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SistemaAlmacen.Shared.Enums;

namespace SistemaAlmacen.Data.Entities;

/// <summary>
/// Representa un comprobante fiscal electrónico emitido por AFIP/ARCA.
/// </summary>
public class Comprobante
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int VentaId { get; set; }

    [Required]
    public int TipoComprobante { get; set; }

    [Required]
    public long NumeroComprobante { get; set; }

    [MaxLength(14)]
    public string? CAE { get; set; }

    public DateTime? FechaVencimientoCAE { get; set; }

    [Required]
    public EstadoComprobante Estado { get; set; }

    [MaxLength(500)]
    public string? ErrorDetalle { get; set; }

    [Required]
    public DateTime FechaEmision { get; set; }

    // Navigation properties
    [ForeignKey(nameof(VentaId))]
    public Venta Venta { get; set; } = null!;
}
