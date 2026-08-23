using System.ComponentModel.DataAnnotations;

namespace SistemaAlmacen.Shared.DTOs.Productos;

/// <summary>
/// Request para actualizar un producto existente.
/// </summary>
public class UpdateProductoRequest
{
    [Required(ErrorMessage = "El nombre es requerido.")]
    [MaxLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres.")]
    public string? Descripcion { get; set; }

    [Required(ErrorMessage = "El precio es requerido.")]
    [Range(0.01, 999999999.99, ErrorMessage = "El precio debe estar entre 0.01 y 999,999,999.99.")]
    public decimal Precio { get; set; }

    [Required(ErrorMessage = "El stock es requerido.")]
    [Range(0, int.MaxValue, ErrorMessage = "El stock debe ser mayor o igual a cero.")]
    public int Stock { get; set; }

    [Required(ErrorMessage = "La categoría es requerida.")]
    public int CategoriaId { get; set; }
}
