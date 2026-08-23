using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaAlmacen.Data.Entities;

/// <summary>
/// Representa el pago (o parte del pago) de una venta mediante un medio de pago específico.
/// </summary>
public class VentaPago
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int VentaId { get; set; }

    [Required]
    public int MedioPagoId { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Monto { get; set; }

    // Navigation properties
    [ForeignKey(nameof(VentaId))]
    public Venta Venta { get; set; } = null!;

    [ForeignKey(nameof(MedioPagoId))]
    public MedioPago MedioPago { get; set; } = null!;
}
