namespace PuntoMarket.Usuarios.Application.Ports.Gestion;

public interface IEnviadorCorreo
{
    Task EnviarActivacionAsync(
        string correo,
        string token,
        DateTimeOffset expira,
        CancellationToken cancellationToken = default);
}
