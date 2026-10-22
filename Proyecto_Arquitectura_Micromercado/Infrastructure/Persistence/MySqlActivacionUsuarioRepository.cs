using MySql.Data.MySqlClient;
using Proyecto_Arquitectura_Micromercado.Application.Security;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.Errores;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;

public sealed class MySqlActivacionUsuarioRepository(MySqlUnidadDeTrabajo unidadDeTrabajo)
    : IActivacionUsuarioRepository
{
    public Task<bool> TokenValidoAsync(
        string tokenHash,
        DateTime ahoraUtc,
        CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync(async () =>
        {
            const string sql = """
                SELECT EXISTS(
                    SELECT 1
                    FROM USUARIO
                    WHERE tokenActivacionHash = @tokenHash
                      AND tokenActivacionExpira > @ahoraUtc
                      AND estado = 'PENDIENTE'
                      AND estaActivo = 0);
                """;
            await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);
            await using var command = new MySqlCommand(sql, alquiler.Conexion, alquiler.Transaccion);
            command.Parameters.AddWithValue("@tokenHash", tokenHash);
            command.Parameters.AddWithValue("@ahoraUtc", ahoraUtc);
            return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 1;
        });

    public Task<bool> ActivarAsync(
        string tokenHash,
        string passwordHash,
        DateTime ahoraUtc,
        CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync(async () =>
        {
            const string sql = """
                UPDATE USUARIO
                SET passwordHash = @passwordHash,
                    estado = 'ACTIVO',
                    estaActivo = 1,
                    debeCambiarPassword = 0,
                    fechaActivacion = @ahoraUtc,
                    tokenActivacionHash = NULL,
                    tokenActivacionExpira = NULL,
                    fechaActualizacion = CURRENT_TIMESTAMP
                WHERE tokenActivacionHash = @tokenHash
                  AND tokenActivacionExpira > @ahoraUtc
                  AND estado = 'PENDIENTE'
                  AND estaActivo = 0;
                """;
            await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);
            await using var command = new MySqlCommand(sql, alquiler.Conexion, alquiler.Transaccion);
            command.Parameters.AddWithValue("@passwordHash", passwordHash);
            command.Parameters.AddWithValue("@tokenHash", tokenHash);
            command.Parameters.AddWithValue("@ahoraUtc", ahoraUtc);
            return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
        });
}
