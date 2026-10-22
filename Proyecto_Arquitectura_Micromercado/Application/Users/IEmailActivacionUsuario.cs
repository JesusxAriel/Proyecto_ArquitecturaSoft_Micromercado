namespace Proyecto_Arquitectura_Micromercado.Application.Users;

public interface IEmailActivacionUsuario
{
    Task EnviarEnlaceAsync(
        string correo,
        string token,
        DateTimeOffset expira,
        CancellationToken cancellationToken = default);
}
