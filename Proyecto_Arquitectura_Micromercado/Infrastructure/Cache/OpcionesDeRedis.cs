using StackExchange.Redis;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Cache;

public static class OpcionesDeRedis
{
    private const int EsperaMaximaEnMilisegundos = 500;

    public static ConfigurationOptions Crear(string conexion)
    {
        var opciones = ConfigurationOptions.Parse(conexion);
        opciones.AbortOnConnectFail = false;
        opciones.ConnectTimeout = EsperaMaximaEnMilisegundos;
        opciones.SyncTimeout = EsperaMaximaEnMilisegundos;
        opciones.AsyncTimeout = EsperaMaximaEnMilisegundos;
        return opciones;
    }
}
