using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SistemaAlmacen.Shared.Enums;

namespace SistemaAlmacen.Data.Entities;

/// <summary>
/// Registro de auditoría que documenta una operación realizada en el sistema.
/// </summary>
public class AuditoriaLog
{
    [Key]
    public long Id { get; set; }

    [Required]
    public int UsuarioId { get; set; }

    [Required]
    public TipoOperacion TipoOperacion { get; set; }

    [Required]
    [MaxLength(100)]
    public string EntidadAfectada { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string RegistroAfectadoId { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Descripcion { get; set; } = string.Empty;

    [Required]
    public DateTime Fecha { get; set; }

    // Navigation properties
    [ForeignKey(nameof(UsuarioId))]
    public Usuario Usuario { get; set; } = null!;
}
