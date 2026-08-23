using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SistemaAlmacen.Shared.Enums;

namespace SistemaAlmacen.Data.Entities;

/// <summary>
/// Representa una sesión de caja registradora con su apertura, movimientos y cierre.
/// </summary>
public class Caja
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PuntoDeVentaId { get; set; }

    [Required]
    public int UsuarioAperturaId { get; set; }

    public int? UsuarioCierreId { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal MontoInicial { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? MontoRealCierre { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? SaldoEsperado { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Diferencia { get; set; }

    [Required]
    public EstadoCaja Estado { get; set; }

    [Required]
    public DateTime FechaApertura { get; set; }

    public DateTime? FechaCierre { get; set; }

    // Navigation properties
    [ForeignKey(nameof(UsuarioAperturaId))]
    public Usuario UsuarioApertura { get; set; } = null!;

    [ForeignKey(nameof(UsuarioCierreId))]
    public Usuario? UsuarioCierre { get; set; }

    public ICollection<CajaMovimiento> Movimientos { get; set; } = new List<CajaMovimiento>();
}
