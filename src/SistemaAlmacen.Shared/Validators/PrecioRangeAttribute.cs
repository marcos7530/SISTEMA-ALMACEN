using System.ComponentModel.DataAnnotations;

namespace SistemaAlmacen.Shared.Validators;

/// <summary>
/// Valida que un campo de precio esté dentro del rango permitido: $0.01 a $999,999,999.99.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class PrecioRangeAttribute : ValidationAttribute
{
    public const double MinValue = 0.01;
    public const double MaxValue = 999_999_999.99;

    public PrecioRangeAttribute()
        : base(ValidationMessages.PrecioFueraDeRango)
    {
    }

    public override bool IsValid(object? value)
    {
        if (value is null)
            return true; // Use [Required] for null checks

        double numericValue = value switch
        {
            decimal d => (double)d,
            double d => d,
            float f => f,
            int i => i,
            long l => l,
            _ => double.NaN
        };

        if (double.IsNaN(numericValue))
            return false;

        return numericValue >= MinValue && numericValue <= MaxValue;
    }

    public override string FormatErrorMessage(string name)
    {
        return ErrorMessageString;
    }
}
