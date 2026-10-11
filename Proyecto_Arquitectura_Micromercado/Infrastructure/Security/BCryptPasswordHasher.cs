using Proyecto_Arquitectura_Micromercado.Application.Security;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Security;

// Adaptador BCrypt del puerto IPasswordHasher.
//
// El factor de trabajo 11 hace que cada verificacion cueste unos 100 ms: imperceptible
// para una persona y caro para quien intente adivinar contrasenas por fuerza bruta. El
// factor queda guardado dentro del propio hash, por lo que se puede subir mas adelante
// sin invalidar los hashes existentes.
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
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }
}
