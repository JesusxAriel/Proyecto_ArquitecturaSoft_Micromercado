using PuntoMarket.Usuarios.Application.Ports.Comunes;

namespace PuntoMarket.Usuarios.Infrastructure.Security;

public sealed class BCryptHasheadorPassword : IHasheadorPassword
{
    private const int FactorDeTrabajo = 11;

    public string Hash(string contrasena) =>
        BCrypt.Net.BCrypt.HashPassword(contrasena, FactorDeTrabajo);

    public bool Verify(string contrasena, string hash)
    {
        if (string.IsNullOrEmpty(contrasena) || string.IsNullOrEmpty(hash))
        {
            return false;
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(contrasena, hash);
        }
        catch (Exception ex) when (ex is BCrypt.Net.SaltParseException or ArgumentException)
        {
            return false;
        }
    }
}
