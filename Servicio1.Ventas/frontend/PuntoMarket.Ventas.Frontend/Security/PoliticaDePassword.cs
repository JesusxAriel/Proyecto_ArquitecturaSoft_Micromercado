namespace PuntoMarket.Ventas.Frontend.Security;

public static class PoliticaDePassword
{
    public const int LongitudMinima = 8;

    public static string? PrimerIncumplimiento(string? contrasena)
    {
        if (string.IsNullOrEmpty(contrasena)) return "Ingrese la nueva contraseña.";
        if (contrasena.Length < LongitudMinima) return "La nueva contraseña debe tener al menos 8 caracteres.";
        if (!contrasena.Any(char.IsUpper)) return "La nueva contraseña debe contener al menos una letra mayúscula.";
        if (!contrasena.Any(char.IsLower)) return "La nueva contraseña debe contener al menos una letra minúscula.";
        if (!contrasena.Any(char.IsDigit)) return "La nueva contraseña debe contener al menos un número.";
        if (contrasena.All(char.IsLetterOrDigit)) return "La nueva contraseña debe contener al menos un carácter especial.";
        return null;
    }
}
