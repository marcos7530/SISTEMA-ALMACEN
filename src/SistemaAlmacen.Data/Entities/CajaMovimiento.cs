using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SistemaAlmacen.Shared.Enums;

namespace SistemaAlmacen.Data.Entities;

/// <summary>
/// Representa un movimiento individual dentro de una sesión de caja.
/// </summary>
public class CajaMovimiento
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CajaId { get; set; }

    [Required]
    public int UsuarioId { get; set; }

    [Required]
    public TipoMovimiento Tipo { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Monto { get; set; }

    [MaxLength(200)]
    public string? Motivo { get; set; }

    [Required]
    public DateTime Fecha { get; set; }

    // Navigation properties
    [ForeignKey(nameof(CajaId))]
    public Caja Caja { get; set; } = null!;

    [ForeignKey(nameof(UsuarioId))]
    public Usuario Usuario { get; set; } = null!;
}
