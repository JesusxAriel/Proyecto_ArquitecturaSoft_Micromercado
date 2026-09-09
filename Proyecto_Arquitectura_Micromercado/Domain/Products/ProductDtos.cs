using System.ComponentModel.DataAnnotations;

namespace Proyecto_Arquitectura_Micromercado.Domain.Products;

public static class ProductPriceValidation
{
    public const string DecimalPattern = @"^\d+([.,]\d{1,2})?$";
    public const string SaleMessage =
        "Formato inválido. Ej: 10,50 o 10,90";
    public const string CostMessage =
        "Formato inválido. Ej: 6,99 o 12,50";
}

public sealed class ProductTextAttribute : ValidationAttribute
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

public sealed class SalePriceAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is not decimal amount)
        {
            return true;
        }

        return amount > 0 && decimal.Round(amount * 10, 0) == amount * 10;
    }

    public override string FormatErrorMessage(string name) =>
        ProductPriceValidation.SaleMessage;
}

public sealed class CostPriceAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) =>
        value is not decimal amount || amount > 0;

    public override string FormatErrorMessage(string name) =>
        ProductPriceValidation.CostMessage;
}
