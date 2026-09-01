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

    /// <summary>Cliente asociado a la venta. Opcional: las ventas de mostrador no tienen cliente.</summary>
    public int? ClienteId { get; set; }

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

    [ForeignKey(nameof(ClienteId))]
    public Cliente? Cliente { get; set; }

    public ICollection<DetalleVenta> Detalles { get; set; } = new List<DetalleVenta>();
    public ICollection<VentaPago> Pagos { get; set; } = new List<VentaPago>();

    /// <summary>
    /// Comprobantes fiscales asociados a la venta (factura y, eventualmente, nota de crédito).
    /// </summary>
    public ICollection<Comprobante> Comprobantes { get; set; } = new List<Comprobante>();
    public ICollection<MovimientoCuentaCorriente> MovimientosCuentaCorriente { get; set; } = new List<MovimientoCuentaCorriente>();
}
