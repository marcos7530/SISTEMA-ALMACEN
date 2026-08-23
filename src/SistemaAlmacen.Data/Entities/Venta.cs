using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SistemaAlmacen.Shared.Enums;

namespace SistemaAlmacen.Data.Entities;

/// <summary>
/// Representa una transacción comercial compuesta por uno o más detalles de venta.
/// </summary>
public class Venta
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UsuarioId { get; set; }

    [Required]
    public DateTime Fecha { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Total { get; set; }

    [Required]
    public EstadoVenta Estado { get; set; }

    [Required]
    public DateTime FechaCreacion { get; set; }

    // Navigation properties
    [ForeignKey(nameof(UsuarioId))]
    public Usuario Usuario { get; set; } = null!;

    public ICollection<DetalleVenta> Detalles { get; set; } = new List<DetalleVenta>();
    public ICollection<VentaPago> Pagos { get; set; } = new List<VentaPago>();
    public Comprobante? Comprobante { get; set; }
}
