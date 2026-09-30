using System.ComponentModel.DataAnnotations;

namespace SistemaAlmacen.Shared.DTOs.Facturacion;

/// <summary>
/// Solicitud para enviar por email el PDF de un comprobante emitido.
/// El email es editable: permite enviar a una dirección indicada en el momento
/// (por ejemplo, la que el cliente proporciona en el mostrador). Si se deja vacío,
/// se usa el email guardado del cliente asociado a la venta.
/// </summary>
public class EnviarComprobanteEmailRequest
{
    /// <summary>
    /// Dirección de correo a la que enviar el comprobante. Opcional: si es nulo o vacío,
    /// se usa el email registrado del cliente.
    /// </summary>
    [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
    [StringLength(254, ErrorMessage = "El correo no puede superar los 254 caracteres.")]
    public string? Email { get; set; }
}
