using PuntoMarket.Reportes.Application.Ports.Comunes;

namespace PuntoMarket.Reportes.Infrastructure.Relojes;

public class RelojDelSistema : IReloj
{
    private const string IdentificadorIana = "America/La_Paz";
    private const string IdentificadorWindows = "SA Western Standard Time";

    private readonly TimeZoneInfo _zonaHoraria = ObtenerZonaHoraria();

    public DateTime Ahora() => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _zonaHoraria);

    private static TimeZoneInfo ObtenerZonaHoraria()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(IdentificadorIana);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById(IdentificadorWindows);
        }
    }
}
