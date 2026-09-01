using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SistemaAlmacen.Shared.Enums;

namespace SistemaAlmacen.Data.Entities;

/// <summary>
/// Registra un movimiento de stock de un producto (ingreso por reposición o baja por rotura,
/// vencimiento, etc.), conservando el historial con motivo y cantidades.
/// </summary>
public class MovimientoStock
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ProductoId { get; set; }

    [Required]
    public int UsuarioId { get; set; }

    [Required]
    public TipoMovimientoStock Tipo { get; set; }

    /// <summary>
    /// Motivo de la baja. Solo aplica cuando <see cref="Tipo"/> es <see cref="TipoMovimientoStock.Baja"/>.
    /// </summary>
    public MotivoBajaStock? Motivo { get; set; }

    /// <summary>
    /// Cantidad de unidades del movimiento (siempre positiva).
    /// </summary>
    [Required]
    public int Cantidad { get; set; }

    [Required]
    public int StockAnterior { get; set; }

    [Required]
    public int StockNuevo { get; set; }

    [MaxLength(200)]
    public string? Observacion { get; set; }

    /// <summary>Compra que originó el movimiento (ingreso por compra o su anulación). Opcional.</summary>
    public int? CompraId { get; set; }

    [Required]
    public DateTime Fecha { get; set; }

    // Navigation properties
    [ForeignKey(nameof(ProductoId))]
    public Producto Producto { get; set; } = null!;

    [ForeignKey(nameof(UsuarioId))]
    public Usuario Usuario { get; set; } = null!;

    [ForeignKey(nameof(CompraId))]
    public Compra? Compra { get; set; }
}
