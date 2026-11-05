namespace PuntoMarket.Usuarios.Application.Ports.Comunes;

public interface IHasheadorPassword
{
    string Hash(string contrasena);

    bool Verify(string contrasena, string hash);
}
