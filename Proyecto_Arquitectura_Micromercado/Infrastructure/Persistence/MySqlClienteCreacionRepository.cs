using MySql.Data.MySqlClient;
using Proyecto_Arquitectura_Micromercado.Application.Customers;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.Errores;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;

public sealed class MySqlClienteCreacionRepository(
    MySqlUnidadDeTrabajo unidadDeTrabajo) : IClienteCreacionRepository
{
    public Task<int> CrearAsync(string nitCi, string razonSocial, int idUsuarioCreacion,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO CLIENTE (nitCi, razonSocial, idUsuarioCreacion)
            VALUES (@nitCi, @razonSocial, @idUsuarioCreacion);
            """;

        return ErroresDePersistencia.TraducirAsync(async () =>
        {
            await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);
            await using var command = new MySqlCommand(sql, alquiler.Conexion, alquiler.Transaccion);
            command.Parameters.AddWithValue("@nitCi", nitCi);
            command.Parameters.AddWithValue("@razonSocial", razonSocial);
            command.Parameters.AddWithValue("@idUsuarioCreacion", idUsuarioCreacion);
            try
            {
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (MySqlException ex) when (ex.Number == 1062 &&
                ex.Message.Contains("UQ_Cliente_nitCi_activo", StringComparison.OrdinalIgnoreCase))
            {
                throw new ClienteDuplicadoException();
            }
            return checked((int)command.LastInsertedId);
        });
    }
}
