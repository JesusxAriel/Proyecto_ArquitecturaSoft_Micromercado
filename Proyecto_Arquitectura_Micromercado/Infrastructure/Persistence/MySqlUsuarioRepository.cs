using MySql.Data.MySqlClient;
using Proyecto_Arquitectura_Micromercado.Application.Security;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.Errores;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;

public sealed class MySqlUsuarioRepository(MySqlUnidadDeTrabajo unidadDeTrabajo) : IUsuarioRepository
{
    public Task<CredencialesDeUsuario?> ObtenerCredencialesAsync(
        string nombreUsuario,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT u.id, u.username, u.nombreCompleto, u.passwordHash, u.estaActivo,
                   r.nombre AS rol
            FROM USUARIO u
            INNER JOIN ROL r ON r.id = u.idRol
            WHERE u.username = @username AND r.estaActivo = 1
            LIMIT 1;
            """;

        return ErroresDePersistencia.TraducirAsync<CredencialesDeUsuario?>(async () =>
        {
            await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);
            await using var command = new MySqlCommand(sql, alquiler.Conexion, alquiler.Transaccion);
            command.Parameters.AddWithValue("@username", nombreUsuario);
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
                reader.GetBoolean(reader.GetOrdinal("estaActivo")));
        });
    }
}
