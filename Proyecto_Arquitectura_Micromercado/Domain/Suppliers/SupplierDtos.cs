using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Proyecto_Arquitectura_Micromercado.Domain.Suppliers;

public static class SupplierValidation
{
    public const string TelefonoPattern = @"^\d{7,15}$";

    public const string TelefonoMessage =
        "El teléfono debe contener entre 7 y 15 dígitos numéricos.";
    public const string CorreoPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
    public const string CorreoMessage =
        "Ingrese un correo electrónico válido con dominio (ejemplo@dominio.com).";
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