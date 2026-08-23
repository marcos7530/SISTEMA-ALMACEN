using System.ComponentModel.DataAnnotations;

namespace SistemaAlmacen.Shared.Validators;

/// <summary>
/// Valida que un campo de stock sea mayor o igual a cero.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class StockRangeAttribute : ValidationAttribute
{
    public StockRangeAttribute()
        : base(ValidationMessages.StockNegativo)
    {
    }

    public override bool IsValid(object? value)
    {
        if (value is null)
            return true; // Use [Required] for null checks

        return value switch
        {
            int i => i >= 0,
            long l => l >= 0,
            decimal d => d >= 0,
            double d => d >= 0,
            float f => f >= 0,
            short s => s >= 0,
            _ => false
        };
    }

    public override string FormatErrorMessage(string name)
    {
        return ErrorMessageString;
    }
}
