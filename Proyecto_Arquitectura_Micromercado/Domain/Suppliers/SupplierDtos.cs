using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Proyecto_Arquitectura_Micromercado.Domain.Suppliers;

public static class SupplierValidation
{
    // Celular boliviano: 8 dígitos que inician con 6 o 7. El prefijo +591 es opcional.
    public const string TelefonoPattern = @"^(\+591\s?)?[67]\d{7}$";

    public const string TelefonoMessage =
        "Ingrese un celular boliviano de 8 dígitos que empiece con 6 o 7. Ej: 71234567 o +591 71234567";
    public const string CorreoMessage =
        "Formato inválido. Ej: ventas@empresa.com.bo";
    public static string NormalizeTelefono(string? value)
    {
        var telefono = (value ?? string.Empty).Trim();
        return telefono.StartsWith("+591", StringComparison.Ordinal)
            ? telefono[4..].Trim()
            : telefono;
    }

    public const string NombreDuplicadoMessage =
        "Ya existe un proveedor registrado con ese nombre de empresa.";
    public const string CorreoRequeridoParaAutogestionadoMessage =
        "Un proveedor autogestionado necesita un correo para coordinar sus reposiciones.";
}

public sealed class SupplierTextAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is not string text || string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        return !text.Any(char.IsControl);
    }

    public override string FormatErrorMessage(string name) =>
        $"El campo {name} debe tener palabras separadas por un solo espacio.";
}

public sealed class TelefonoAttribute : ValidationAttribute
{
    private static readonly Regex Pattern =
        new(SupplierValidation.TelefonoPattern, RegexOptions.CultureInvariant);

    public override bool IsValid(object? value)
    {
        if (value is not string text || string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        return Pattern.IsMatch(text.Trim());
    }

    public override string FormatErrorMessage(string name) =>
        SupplierValidation.TelefonoMessage;
}