using MySql.Data.MySqlClient;
using PuntoMarket.Ventas.Application.Common;

namespace PuntoMarket.Ventas.Infrastructure.Persistence;

public sealed class DetectorDeInterbloqueoMySql : IDetectorDeInterbloqueo
{
    private const int ErrorDeInterbloqueo = 1213;

    public bool EsInterbloqueo(Exception excepcion)
    {
        for (var actual = excepcion; actual is not null; actual = actual.InnerException)
        {
            if (actual is MySqlException { Number: ErrorDeInterbloqueo })
            {
                return true;
            }
        }

        return false;
    }
}
