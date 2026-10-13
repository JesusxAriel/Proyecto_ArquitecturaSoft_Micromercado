using MySql.Data.MySqlClient;
using Proyecto_Arquitectura_Micromercado.Application.Common;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.Errores;

internal static class ErroresDePersistencia
{
    public static async Task<T> TraducirAsync<T>(Func<Task<T>> operacion)
    {
        try
        {
            return await operacion();
        }
        catch (MySqlException ex)
        {
            throw new ErrorDePersistenciaException(ex);
        }
    }

    public static async Task TraducirAsync(Func<Task> operacion)
    {
        try
        {
            await operacion();
        }
        catch (MySqlException ex)
        {
            throw new ErrorDePersistenciaException(ex);
        }
    }
}
