namespace PuntoMarket.Usuarios.Application.Ports.Comunes;

public interface IReloj
{
    DateTimeOffset UtcNow { get; }
}
