using System.Text.RegularExpressions;

namespace Proyecto_Arquitectura_Micromercado.Validations
{
    public static class Validation
    {
        public static bool IsRequired(string? value)
        {
            return !string.IsNullOrWhiteSpace(value);
        }

        public static bool HasValidLength(string? value, int maxLength)
        {
            return value != null && value.Length <= maxLength;
        }

        public static bool IsValidEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return true;
            return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        public static bool IsValidPhoneNumber(string? number)
        {
            return !string.IsNullOrWhiteSpace(number) && Regex.IsMatch(number, @"^[0-9]{6,20}$");
        }
    }
}