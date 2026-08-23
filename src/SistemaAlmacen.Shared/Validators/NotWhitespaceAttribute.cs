using System.ComponentModel.DataAnnotations;

namespace SistemaAlmacen.Shared.Validators;

/// <summary>
/// Valida que un string no sea nulo, vacío o compuesto solo de espacios en blanco.
/// A diferencia de [Required], este atributo rechaza strings que contienen solo whitespace.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class NotWhitespaceAttribute : ValidationAttribute
{
    public NotWhitespaceAttribute()
        : base(ValidationMessages.CampoRequerido)
    {
    }

    public override bool IsValid(object? value)
    {
        if (value is null)
            return false;

        if (value is string stringValue)
            return !string.IsNullOrWhiteSpace(stringValue);

        return true;
    }

    public override string FormatErrorMessage(string name)
    {
        return string.Format(ErrorMessageString, name);
    }
}
