using System.ComponentModel.DataAnnotations;

namespace SistemaAlmacen.Shared.DTOs.Categorias;

/// <summary>
/// Request para actualizar una categoría existente.
/// </summary>
public class UpdateCategoriaRequest
{
    [Required(ErrorMessage = "El nombre es requerido.")]
    [MinLength(3, ErrorMessage = "El nombre debe tener al menos 3 caracteres.")]
    [MaxLength(50, ErrorMessage = "El nombre no puede exceder 50 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(200, ErrorMessage = "La descripción no puede exceder 200 caracteres.")]
    public string? Descripcion { get; set; }

    [Range(0, 999.99, ErrorMessage = "El margen debe estar entre 0 y 999.99.")]
    public decimal? MargenGanancia { get; set; }
}
