using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SistemaAlmacen.Shared.Enums;

namespace SistemaAlmacen.Data.Entities;

/// <summary>
/// Representa un cliente del comercio, con sus datos fiscales y de contacto.
/// Puede operar con cuenta corriente (fiado) hasta un límite de crédito configurable.
/// </summary>
public class Cliente
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(150)]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Documento fiscal: CUIT (11 dígitos) o DNI. Opcional para consumidor final.</summary>
    [MaxLength(20)]
    public string? Documento { get; set; }

    [Required]
    public CondicionIva CondicionIva { get; set; } = CondicionIva.ConsumidorFinal;

    [MaxLength(254)]
    [EmailAddress]
    public string? Email { get; set; }

    [MaxLength(30)]
    public string? Telefono { get; set; }

    [MaxLength(250)]
    public string? Direccion { get; set; }

    /// <summary>Habilita la operatoria de cuenta corriente (fiado) para este cliente.</summary>
    [Required]
    public bool CuentaCorrienteHabilitada { get; set; }

    /// <summary>
    /// Límite máximo de deuda permitido en cuenta corriente. 0 = sin límite.
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal LimiteCredito { get; set; }

    [Required]
    public bool Activo { get; set; } = true;

    [Required]
    public DateTime FechaCreacion { get; set; }

    [Required]
    public DateTime FechaModificacion { get; set; }

    // Navigation properties
    public ICollection<Venta> Ventas { get; set; } = new List<Venta>();
    public ICollection<MovimientoCuentaCorriente> MovimientosCuentaCorriente { get; set; } = new List<MovimientoCuentaCorriente>();
}
