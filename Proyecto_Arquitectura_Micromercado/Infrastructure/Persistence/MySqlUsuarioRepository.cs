using MySql.Data.MySqlClient;
using Proyecto_Arquitectura_Micromercado.Application.Security;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.Errores;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;

public sealed class MySqlUsuarioRepository(MySqlUnidadDeTrabajo unidadDeTrabajo) : IUsuarioRepository
{
    public Task<CredencialesDeUsuario?> ObtenerCredencialesAsync(
        string correo,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT u.id, u.username, u.nombreCompleto, u.passwordHash, u.estaActivo,
                   u.estado, u.debeCambiarPassword, r.nombre AS rol
            FROM USUARIO u
            INNER JOIN ROL r ON r.id = u.idRol
            WHERE u.correo = @correo AND r.estaActivo = 1
            LIMIT 1;
            """;

        return ErroresDePersistencia.TraducirAsync<CredencialesDeUsuario?>(async () =>
        {
            await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);
            await using var command = new MySqlCommand(sql, alquiler.Conexion, alquiler.Transaccion);
            command.Parameters.AddWithValue("@correo", correo);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return new CredencialesDeUsuario(
                reader.GetInt32(reader.GetOrdinal("id")),
                reader.GetString(reader.GetOrdinal("username")),
                reader.GetString(reader.GetOrdinal("nombreCompleto")),
                reader.GetString(reader.GetOrdinal("rol")),
                reader.GetString(reader.GetOrdinal("passwordHash")),
                reader.GetBoolean(reader.GetOrdinal("estaActivo")),
                reader.GetString(reader.GetOrdinal("estado")),
                reader.GetBoolean(reader.GetOrdinal("debeCambiarPassword")));
        });
    }

    public Task<CredencialesDeUsuario?> ObtenerCredencialesPorIdAsync(
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT u.id, u.username, u.nombreCompleto, u.passwordHash, u.estaActivo,
                   u.estado, u.debeCambiarPassword, r.nombre AS rol
            FROM USUARIO u
            INNER JOIN ROL r ON r.id = u.idRol
            WHERE u.id = @id
            LIMIT 1;
            """;

        return ErroresDePersistencia.TraducirAsync<CredencialesDeUsuario?>(async () =>
        {
            await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);
            await using var command = new MySqlCommand(sql, alquiler.Conexion, alquiler.Transaccion);
            command.Parameters.AddWithValue("@id", usuarioId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return new CredencialesDeUsuario(
                reader.GetInt32(reader.GetOrdinal("id")),
                reader.GetString(reader.GetOrdinal("username")),
                reader.GetString(reader.GetOrdinal("nombreCompleto")),
                reader.GetString(reader.GetOrdinal("rol")),
                reader.GetString(reader.GetOrdinal("passwordHash")),
                reader.GetBoolean(reader.GetOrdinal("estaActivo")),
                reader.GetString(reader.GetOrdinal("estado")),
                reader.GetBoolean(reader.GetOrdinal("debeCambiarPassword")));
        });
    }

    public Task ActualizarPasswordAsync(int usuarioId, string nuevoPasswordHash, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE USUARIO 
            SET passwordHash = @passwordHash, 
                debeCambiarPassword = 0, 
                fechaActualizacion = CURRENT_TIMESTAMP
            WHERE id = @id;
            """;

        return ErroresDePersistencia.TraducirAsync(async () =>
        {
            await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);
            await using var command = new MySqlCommand(sql, alquiler.Conexion, alquiler.Transaccion);
            command.Parameters.AddWithValue("@passwordHash", nuevoPasswordHash);
            command.Parameters.AddWithValue("@id", usuarioId);
            await command.ExecuteNonQueryAsync(cancellationToken);
        });
    }

    public Task<bool> EstaActivoAsync(int usuarioId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT estaActivo FROM USUARIO WHERE id = @id LIMIT 1;";

        return ErroresDePersistencia.TraducirAsync(async () =>
        {
            await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);
            await using var command = new MySqlCommand(sql, alquiler.Conexion, alquiler.Transaccion);
            command.Parameters.AddWithValue("@id", usuarioId);
            var result = await command.ExecuteScalarAsync(cancellationToken);

            return result != null && Convert.ToBoolean(result);
        });
    }
}