using System.ComponentModel.DataAnnotations;

namespace SistemaAlmacen.Shared.DTOs.Productos;

/// <summary>
/// Request para incrementar (reponer) stock de un producto identificado por su código de barras.
/// </summary>
public class ReponerStockRequest
{
    [Required(ErrorMessage = "El código de barras es requerido.")]
    [MaxLength(50, ErrorMessage = "El código de barras no puede exceder 50 caracteres.")]
    public string CodigoBarras { get; set; } = string.Empty;

    [Required(ErrorMessage = "La cantidad es requerida.")]
    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor o igual a uno.")]
    public int Cantidad { get; set; }

    [MaxLength(200, ErrorMessage = "La observación no puede exceder 200 caracteres.")]
    public string? Observacion { get; set; }
}
