using Proyecto_Arquitectura_Micromercado.Application.Security;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Security;

public sealed class BCryptPasswordHasher : IPasswordHasher
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
