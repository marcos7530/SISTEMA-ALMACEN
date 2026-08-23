using System.ComponentModel.DataAnnotations;
using SistemaAlmacen.Shared.Enums;

namespace SistemaAlmacen.Data.Entities;

/// <summary>
/// Representa un usuario del sistema con credenciales de acceso y rol asignado.
/// </summary>
public class Usuario
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [MaxLength(254)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    public Rol Rol { get; set; }

    [Required]
    public bool Activo { get; set; } = true;

    [Required]
    public int IntentosFallidos { get; set; }

    public DateTime? BloqueadoHasta { get; set; }

    [Required]
    public DateTime FechaCreacion { get; set; }

    // Navigation properties
    public ICollection<Venta> Ventas { get; set; } = new List<Venta>();
    public ICollection<AuditoriaLog> AuditoriaLogs { get; set; } = new List<AuditoriaLog>();
    public ICollection<TokenRecuperacion> TokensRecuperacion { get; set; } = new List<TokenRecuperacion>();
    public ICollection<CajaMovimiento> CajaMovimientos { get; set; } = new List<CajaMovimiento>();
    public ICollection<Caja> CajasAbiertas { get; set; } = new List<Caja>();
    public ICollection<Caja> CajasCerradas { get; set; } = new List<Caja>();
}
