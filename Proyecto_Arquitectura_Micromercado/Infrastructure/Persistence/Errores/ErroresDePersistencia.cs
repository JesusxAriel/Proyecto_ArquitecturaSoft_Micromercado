using MySql.Data.MySqlClient;
using Proyecto_Arquitectura_Micromercado.Application.Common;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.Errores;

// Traduce los errores del driver de MySQL a la excepcion de Application.
//
// Las excepciones de regla de negocio que los repositorios ya lanzan (por ejemplo
// DuplicateProductException, derivada de ArgumentException) no se tocan: pasan de largo
// para que las paginas las sigan mostrando junto al campo que las origina.
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
