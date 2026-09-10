using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Proyecto_Arquitectura_Micromercado.Domain.Categories
{
    public static class CategoryValidation
    {
        public const string NamePattern =
            @"^(?=.*[A-Za-zÁÉÍÓÚáéíóúÑñ])[A-Za-zÁÉÍÓÚáéíóúÑñ0-9\s\-&]+$";

        public const string CodePattern =
            @"^[A-Za-z0-9\-]+$";

        public const string AislePattern =
            @"^Pasillo\s[1-8]$";

        public const string DescriptionPattern =
            @"^[A-Za-zÁÉÍÓÚáéíóúÑñ0-9\s.,\-()]+$";

        public const string NameMessage =
            "El nombre debe contener letras y solo puede incluir letras, números, espacios, guiones y &.";

        public const string CodeMessage =
            "El código solo puede contener letras, números y guiones. Ej: CAT-LAC.";

        public const string AisleMessage =
            "La ubicación debe tener el formato Pasillo 1 hasta Pasillo 8.";

        public const string DescriptionMessage =
            "La descripción solo puede contener letras, números, espacios, puntos, comas, guiones y paréntesis.";

    }

    public sealed class CategoryNameAttribute : ValidationAttribute
    {
        private static readonly Regex Pattern =
            new(
                CategoryValidation.NamePattern,
                RegexOptions.CultureInvariant);

        public override bool IsValid(object? value)
        {
            if (value is not string text ||
                string.IsNullOrWhiteSpace(text))
            {
                return true;
            }

            return Pattern.IsMatch(text.Trim());
        }

        public override string FormatErrorMessage(string name) =>
            CategoryValidation.NameMessage;
    }

    public sealed class CategoryCodeAttribute : ValidationAttribute
    {
        private static readonly Regex Pattern =
            new(
                CategoryValidation.CodePattern,
                RegexOptions.CultureInvariant);

        public override bool IsValid(object? value)
        {
            if (value is not string text ||
                string.IsNullOrWhiteSpace(text))
            {
                return true;
            }

            return Pattern.IsMatch(text.Trim());
        }

        public override string FormatErrorMessage(string name) =>
            CategoryValidation.CodeMessage;
    }

    public sealed class AisleLocationAttribute : ValidationAttribute
    {
        private static readonly Regex Pattern =
            new(
                CategoryValidation.AislePattern,
                RegexOptions.IgnoreCase |
                RegexOptions.CultureInvariant);

        public override bool IsValid(object? value)
        {
            if (value is not string text ||
                string.IsNullOrWhiteSpace(text))
            {
                return true;
            }

            return Pattern.IsMatch(text.Trim());
        }

        public override string FormatErrorMessage(string name) =>
            CategoryValidation.AisleMessage;
    }

    public sealed class CategoryDescriptionAttribute : ValidationAttribute
    {
        private static readonly Regex Pattern =
            new(
                CategoryValidation.DescriptionPattern,
                RegexOptions.CultureInvariant);

        public override bool IsValid(object? value)
        {
            if (value is not string text ||
                string.IsNullOrWhiteSpace(text))
            {
                return true;
            }

            return Pattern.IsMatch(text.Trim());
        }

        public override string FormatErrorMessage(string name) =>
            CategoryValidation.DescriptionMessage;
    }
}