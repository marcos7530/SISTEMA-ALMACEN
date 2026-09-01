using System.ComponentModel.DataAnnotations;

namespace SistemaAlmacen.Shared.DTOs.Compras;

/// <summary>
/// Request para registrar una compra/ingreso de mercadería.
/// </summary>
public class CreateCompraRequest
{
    [Required(ErrorMessage = "El proveedor es obligatorio.")]
    public int ProveedorId { get; set; }

    [StringLength(50, ErrorMessage = "El número de comprobante no puede superar los 50 caracteres.")]
    public string? NumeroComprobante { get; set; }

    [Required(ErrorMessage = "Debe incluir al menos un producto.")]
    [MinLength(1, ErrorMessage = "Debe incluir al menos un producto.")]
    public List<DetalleCompraRequest> Detalles { get; set; } = new();
}
