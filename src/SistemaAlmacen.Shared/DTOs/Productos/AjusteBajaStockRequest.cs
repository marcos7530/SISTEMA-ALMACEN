using System.ComponentModel.DataAnnotations;
using SistemaAlmacen.Shared.Enums;

namespace SistemaAlmacen.Shared.DTOs.Productos;

/// <summary>
/// Request para dar de baja stock de un producto por un motivo determinado.
/// </summary>
public class AjusteBajaStockRequest
{
    [Required(ErrorMessage = "La cantidad es requerida.")]
    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor o igual a uno.")]
    public int Cantidad { get; set; }

    [Required(ErrorMessage = "El motivo es requerido.")]
    [EnumDataType(typeof(MotivoBajaStock), ErrorMessage = "El motivo seleccionado no es válido.")]
    public MotivoBajaStock Motivo { get; set; }

    [MaxLength(200, ErrorMessage = "La observación no puede exceder 200 caracteres.")]
    public string? Observacion { get; set; }
}
