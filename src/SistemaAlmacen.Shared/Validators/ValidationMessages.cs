namespace SistemaAlmacen.Shared.Validators;

/// <summary>
/// Mensajes de validación estándar en español para uso compartido entre cliente y servidor.
/// </summary>
public static class ValidationMessages
{
    public const string CampoRequerido = "El campo {0} es obligatorio";
    public const string LongitudMaxima = "El campo {0} no puede exceder {1} caracteres";
    public const string LongitudMinima = "El campo {0} debe tener al menos {1} caracteres";
    public const string RangoNumerico = "El campo {0} debe estar entre {1} y {2}";
    public const string FormatoEmailInvalido = "El formato del correo electrónico no es válido";
    public const string PrecioFueraDeRango = "El precio debe estar entre $0.01 y $999,999,999.99";
    public const string StockNegativo = "El stock debe ser mayor o igual a cero";
    public const string CantidadFueraDeRango = "La cantidad debe estar entre 1 y 10,000";
    public const string ContraseñaRequisitos = "La contraseña debe tener entre 8 y 50 caracteres, al menos una mayúscula, una minúscula y un número";
    public const string NombreLongitud = "El nombre debe tener entre {0} y {1} caracteres";
}
