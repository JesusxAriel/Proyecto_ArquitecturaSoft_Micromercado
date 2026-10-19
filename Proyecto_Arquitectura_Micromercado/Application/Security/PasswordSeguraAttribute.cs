using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Proyecto_Arquitectura_Micromercado.Application.Security;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class PasswordSeguraAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string password || string.IsNullOrWhiteSpace(password))
        {
            return new ValidationResult("La contraseña es obligatoria.");
        }

        // Reglas de la Parte 2:
        // - Mínimo 8 caracteres
        // - 1 mayúscula
        // - 1 minúscula
        // - 1 número
        // - 1 carácter especial
        if (password.Length < 8)
        {
            return new ValidationResult("La contraseña debe tener al menos 8 caracteres.");
        }
        if (!Regex.IsMatch(password, @"[A-Z]"))
        {
            return new ValidationResult("La contraseña debe contener al menos una letra mayúscula.");
        }
        if (!Regex.IsMatch(password, @"[a-z]"))
        {
            return new ValidationResult("La contraseña debe contener al menos una letra minúscula.");
        }
        if (!Regex.IsMatch(password, @"[0-9]"))
        {
            return new ValidationResult("La contraseña debe contener al menos un número.");
        }
        if (!Regex.IsMatch(password, @"[\W_]"))
        {
            return new ValidationResult("La contraseña debe contener al menos un carácter especial.");
        }

        return ValidationResult.Success;
    }
}