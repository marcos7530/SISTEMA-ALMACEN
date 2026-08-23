using System.ComponentModel.DataAnnotations;

namespace SistemaAlmacen.Shared.Validators;

/// <summary>
/// Valida que una contraseña cumpla los requisitos de complejidad:
/// mínimo 8 caracteres, máximo 50, al menos una mayúscula, una minúscula y un dígito.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class PasswordComplexityAttribute : ValidationAttribute
{
    public int MinLength { get; } = 8;
    public int MaxLength { get; } = 50;

    public PasswordComplexityAttribute()
        : base(ValidationMessages.ContraseñaRequisitos)
    {
    }

    public override bool IsValid(object? value)
    {
        if (value is not string password)
            return false;

        if (password.Length < MinLength || password.Length > MaxLength)
            return false;

        bool hasUppercase = false;
        bool hasLowercase = false;
        bool hasDigit = false;

        foreach (var c in password)
        {
            if (char.IsUpper(c)) hasUppercase = true;
            else if (char.IsLower(c)) hasLowercase = true;
            else if (char.IsDigit(c)) hasDigit = true;

            if (hasUppercase && hasLowercase && hasDigit)
                return true;
        }

        return hasUppercase && hasLowercase && hasDigit;
    }

    public override string FormatErrorMessage(string name)
    {
        return ErrorMessageString;
    }
}
