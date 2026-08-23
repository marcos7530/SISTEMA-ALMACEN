using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaAlmacen.Data.Entities;

/// <summary>
/// Token de un solo uso para recuperación de contraseña.
/// </summary>
public class TokenRecuperacion
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UsuarioId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Token { get; set; } = string.Empty;

    [Required]
    public bool Usado { get; set; }

    [Required]
    public DateTime FechaCreacion { get; set; }

    [Required]
    public DateTime FechaExpiracion { get; set; }

    // Navigation properties
    [ForeignKey(nameof(UsuarioId))]
    public Usuario Usuario { get; set; } = null!;
}
