namespace PuntoMarket.Usuarios.Domain.Services;

public static class PoliticaDePassword
{
    public static bool CumpleRequisitos(string? contrasena)
    {
        if (string.IsNullOrWhiteSpace(contrasena))
            return false;

        return contrasena.Length >= 8
            && contrasena.Any(char.IsUpper)
            && contrasena.Any(char.IsLower)
            && contrasena.Any(char.IsDigit)
            && contrasena.Any(c => !char.IsLetterOrDigit(c));
    }
}