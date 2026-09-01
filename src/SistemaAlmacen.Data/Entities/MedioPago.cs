using System.ComponentModel.DataAnnotations;

namespace SistemaAlmacen.Data.Entities;

/// <summary>
/// Representa un medio de pago disponible para abonar ventas.
/// </summary>
public class MedioPago
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    public bool Activo { get; set; } = true;

    [Required]
    public bool EsSistema { get; set; }

    /// <summary>
    /// Indica que este medio de pago corresponde a venta en cuenta corriente (fiado).
    /// Al usarse, genera un cargo en la cuenta del cliente en lugar de ingresar a caja.
    /// </summary>
    [Required]
    public bool EsCuentaCorriente { get; set; }

    // Navigation properties
    public ICollection<VentaPago> VentaPagos { get; set; } = new List<VentaPago>();
}
