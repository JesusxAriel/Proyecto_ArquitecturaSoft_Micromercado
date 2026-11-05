using PuntoMarket.Usuarios.Application.Ports.Comunes;

namespace PuntoMarket.Usuarios.Infrastructure.Time;

public sealed class RelojSistema : IReloj
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
