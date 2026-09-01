using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SistemaAlmacen.Shared.Enums;

namespace SistemaAlmacen.Data.Entities;

/// <summary>
/// Representa un movimiento en la cuenta corriente de un cliente.
/// El saldo del cliente se obtiene sumando los cargos y restando los pagos.
/// </summary>
public class MovimientoCuentaCorriente
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ClienteId { get; set; }

    [Required]
    public TipoMovimientoCuentaCorriente Tipo { get; set; }

    /// <summary>Monto del movimiento, siempre positivo. El signo lo determina el Tipo.</summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Monto { get; set; }

    /// <summary>Venta asociada, cuando el movimiento se origina en una venta a crédito o su anulación.</summary>
    public int? VentaId { get; set; }

    /// <summary>Usuario que registró el movimiento.</summary>
    [Required]
    public int UsuarioId { get; set; }

    [MaxLength(250)]
    public string? Descripcion { get; set; }

    [Required]
    public DateTime Fecha { get; set; }

    // Navigation properties
    [ForeignKey(nameof(ClienteId))]
    public Cliente Cliente { get; set; } = null!;

    [ForeignKey(nameof(VentaId))]
    public Venta? Venta { get; set; }

    [ForeignKey(nameof(UsuarioId))]
    public Usuario Usuario { get; set; } = null!;
}
