namespace Proyecto_Arquitectura_Micromercado.Application.Security;

public interface IPasswordHasher
{
    string Hash(string contrasena);

    bool Verify(string contrasena, string hash);
}
