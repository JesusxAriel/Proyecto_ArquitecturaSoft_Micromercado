namespace PuntoMarket.Usuarios.Application.Ports.Comunes;

public interface IUsuarioActual
{
    int Id { get; }

    string Rol { get; }
}
