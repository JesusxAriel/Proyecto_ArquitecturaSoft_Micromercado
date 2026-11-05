using MySql.Data.MySqlClient;
using PuntoMarket.Usuarios.Application.Ports.Autenticacion;
using PuntoMarket.Usuarios.Domain.Enums;

namespace PuntoMarket.Usuarios.Infrastructure.Persistence;

public sealed class MySqlUsuarioAutenticacionRepositorio(ConexionMySql conexionMySql)
    : IUsuarioAutenticacionRepositorio
{
    public async Task<UsuarioAutenticacionDto?> ObtenerPorCorreoAsync(
        string correo,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT u.id, u.username, u.passwordHash, u.nombreCompleto, u.estado,
                   u.debeCambiarPassword, r.nombre AS rol, r.estaActivo AS rolActivo
            FROM USUARIO u
            INNER JOIN ROL r ON r.id = u.idRol
            WHERE u.correo = @correo
            LIMIT 1;
            """;

        await using var conexion = conexionMySql.CrearConexion();
        await conexion.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, conexion);
        command.Parameters.AddWithValue("@correo", correo);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return new UsuarioAutenticacionDto(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            Enum.Parse<EstadoUsuario>(reader.GetString(4)),
            reader.GetBoolean(5),
            reader.GetString(6),
            reader.GetBoolean(7));
    }

    public async Task<UsuarioAutenticacionDto?> ObtenerPorIdAsync(
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT u.id, u.username, u.passwordHash, u.nombreCompleto, u.estado,
                   u.debeCambiarPassword, r.nombre AS rol, r.estaActivo AS rolActivo
            FROM USUARIO u
            INNER JOIN ROL r ON r.id = u.idRol
            WHERE u.id = @usuarioId
            LIMIT 1;
            """;

        await using var conexion = conexionMySql.CrearConexion();
        await conexion.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, conexion);
        command.Parameters.AddWithValue("@usuarioId", usuarioId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return new UsuarioAutenticacionDto(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            Enum.Parse<EstadoUsuario>(reader.GetString(4)),
            reader.GetBoolean(5),
            reader.GetString(6),
            reader.GetBoolean(7));
    }

    public async Task<bool> ActualizarPasswordAsync(
        int usuarioId,
        int idUsuarioActualizacion,
        string passwordHashActual,
        string passwordHash,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE USUARIO
            SET passwordHash = @passwordHash,
                debeCambiarPassword = 0,
                idUsuarioActualizacion = @idUsuarioActualizacion,
                fechaActualizacion = CURRENT_TIMESTAMP
            WHERE id = @usuarioId
              AND passwordHash = @passwordHashActual
              AND estado = 'ACTIVO'
              AND estaActivo = 1
              AND EXISTS (
                  SELECT 1
                  FROM ROL r
                  WHERE r.id = USUARIO.idRol
                    AND r.estaActivo = 1);
            """;
        await using var conexion = conexionMySql.CrearConexion();
        await conexion.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, conexion);
        command.Parameters.AddWithValue("@passwordHash", passwordHash);
        command.Parameters.AddWithValue("@idUsuarioActualizacion", idUsuarioActualizacion);
        command.Parameters.AddWithValue("@passwordHashActual", passwordHashActual);
        command.Parameters.AddWithValue("@usuarioId", usuarioId);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }
}
