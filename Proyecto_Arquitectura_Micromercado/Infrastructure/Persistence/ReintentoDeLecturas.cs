using System.Net.Sockets;
using MySql.Data.MySqlClient;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;

public static class ReintentoDeLecturas
{
    private static readonly TimeSpan EsperaPorDefecto = TimeSpan.FromMilliseconds(150);

    public static bool EsTransitoria(MySqlException ex) =>
        ex.IsTransient
        || ex.Number is 1040 or 1042 or 1053 or 1205 or 1213 or 2002 or 2003 or 2006 or 2013 or 2014 or 2055
        || ex.InnerException is IOException or SocketException or TimeoutException
        || ex.Message.Contains("Fatal error encountered", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("Unable to connect", StringComparison.OrdinalIgnoreCase);

    public static async Task<T> EjecutarAsync<T>(
        Func<Task<T>> lectura,
        TimeSpan? espera = null,
        Func<TimeSpan, Task>? esperar = null)
    {
        try
        {
            return await lectura();
        }
        catch (MySqlException ex) when (EsTransitoria(ex))
        {
            await (esperar ?? (tiempo => Task.Delay(tiempo)))(espera ?? EsperaPorDefecto);
            return await lectura();
        }
    }
}
