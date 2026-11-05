using MySql.Data.MySqlClient;
using PuntoMarket.Usuarios.Application.Ports.Gestion;
using PuntoMarket.Usuarios.Domain.Enums;
using PuntoMarket.Usuarios.Domain.Exceptions;
using PuntoMarket.Usuarios.Domain.Services;

namespace PuntoMarket.Usuarios.Infrastructure.Persistence;

public sealed class MySqlUsuarioGestionRepositorio(ConexionMySql conexionMySql)
    : IUsuarioGestionRepositorio
{
    public async Task<IReadOnlyList<UsuarioListadoDto>> ListarAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT u.id, COALESCE(u.nombre, u.nombreCompleto) AS nombre,
                   COALESCE(u.apellido, '') AS apellido, COALESCE(u.correo, '') AS correo,
                   u.username, r.nombre AS rol, u.estado, u.fechaCreacion,
                   u.tokenActivacionExpira, u.fechaActivacion
            FROM USUARIO u INNER JOIN ROL r ON r.id = u.idRol
            ORDER BY u.id;
            """;
        await using var conexion = conexionMySql.CrearConexion();
        await conexion.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, conexion);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var usuarios = new List<UsuarioListadoDto>();
        while (await reader.ReadAsync(cancellationToken))
        {
            usuarios.Add(new UsuarioListadoDto(
                reader.GetInt32(0), reader.GetString(1), reader.GetString(2),
                reader.GetString(3), reader.GetString(4), reader.GetString(5),
                reader.GetString(6), reader.GetDateTime(7),
                reader.IsDBNull(8) ? null : reader.GetDateTime(8),
                reader.IsDBNull(9) ? null : reader.GetDateTime(9)));
        }
        return usuarios;
    }

    public async Task<IReadOnlyList<RolActivoDto>> ListarRolesActivosAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT id, nombre FROM ROL WHERE estaActivo = 1 ORDER BY nombre;";
        await using var conexion = conexionMySql.CrearConexion();
        await conexion.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, conexion);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var roles = new List<RolActivoDto>();
        while (await reader.ReadAsync(cancellationToken))
            roles.Add(new RolActivoDto(reader.GetInt32(0), reader.GetString(1)));
        return roles;
    }

    public Task<bool> ExisteUsernameAsync(string username, CancellationToken cancellationToken = default) =>
        ExisteAsync("SELECT EXISTS(SELECT 1 FROM USUARIO WHERE username = @valor);", username, cancellationToken);

    public Task<bool> ExisteCorreoAsync(string correo, CancellationToken cancellationToken = default) =>
        ExisteAsync("SELECT EXISTS(SELECT 1 FROM USUARIO WHERE correo = @valor);", correo, cancellationToken);

    public Task<bool> EsRolActivoAsync(int idRol, CancellationToken cancellationToken = default) =>
        ExisteAsync("SELECT EXISTS(SELECT 1 FROM ROL WHERE id = @valor AND estaActivo = 1);", idRol, cancellationToken);

    private async Task<bool> ExisteAsync(string sql, object valor, CancellationToken cancellationToken)
    {
        await using var conexion = conexionMySql.CrearConexion();
        await conexion.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, conexion);
        command.Parameters.AddWithValue("@valor", valor);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 1;
    }

    public async Task<int> CrearAsync(UsuarioAltaInterna usuario, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO USUARIO
                (nombre, apellido, nombreCompleto, correo, username, idRol, passwordHash,
                 estado, estaActivo, debeCambiarPassword, idUsuarioCreacion,
                 tokenActivacionHash, tokenActivacionExpira, fechaCreacion)
            SELECT @nombre, @apellido, @nombreCompleto, @correo, @username, id, @hash,
                   'PENDIENTE', 0, 1, @creador, @tokenHash, @tokenExpira, CURRENT_TIMESTAMP
            FROM ROL WHERE id = @rol AND estaActivo = 1;
            """;
        await using var conexion = conexionMySql.CrearConexion();
        await conexion.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, conexion);
        command.Parameters.AddWithValue("@nombre", usuario.Nombre);
        command.Parameters.AddWithValue("@apellido", usuario.Apellido);
        command.Parameters.AddWithValue("@nombreCompleto", $"{usuario.Nombre} {usuario.Apellido}");
        command.Parameters.AddWithValue("@correo", usuario.Correo);
        command.Parameters.AddWithValue("@username", usuario.Username);
        command.Parameters.AddWithValue("@rol", usuario.IdRol);
        command.Parameters.AddWithValue("@hash", usuario.HashContrasena);
        command.Parameters.AddWithValue("@creador", usuario.IdUsuarioCreacion);
        command.Parameters.AddWithValue("@tokenHash", usuario.TokenActivacionHash);
        command.Parameters.AddWithValue("@tokenExpira", ZonaHorariaDeLaPaz.ALocal(usuario.TokenActivacionExpira));
        try
        {
            if (await command.ExecuteNonQueryAsync(cancellationToken) != 1)
                throw new ArgumentException("Debe seleccionar un rol existente y activo.");
        }
        catch (MySqlException ex) when (ex.Number == 1062 &&
            (ex.Message.Contains("UQ_Usuario_correo", StringComparison.OrdinalIgnoreCase) ||
             ex.Message.Contains("UQ_Usuario_username", StringComparison.OrdinalIgnoreCase)))
        {
            throw new UsuarioDuplicadoException();
        }
        return checked((int)command.LastInsertedId);
    }

    public async Task<UsuarioActivacionDto?> ObtenerParaActivacionAsync(
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, COALESCE(correo, '') AS correo, estado,
                   tokenActivacionHash, tokenActivacionExpira, fechaActivacion
            FROM USUARIO WHERE id = @id;
            """;
        await using var conexion = conexionMySql.CrearConexion();
        await conexion.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, conexion);
        command.Parameters.AddWithValue("@id", usuarioId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;
        return new UsuarioActivacionDto(
            reader.GetInt32(0), reader.GetString(1), Enum.Parse<EstadoUsuario>(reader.GetString(2)),
            reader.IsDBNull(3) ? null : reader.GetString(3),
            reader.IsDBNull(4) ? null : reader.GetDateTime(4),
            reader.IsDBNull(5) ? null : reader.GetDateTime(5));
    }

    public async Task<string?> RenovarTokenActivacionAsync(
        int usuarioId,
        string tokenHash,
        DateTime tokenExpiraUtc,
        int idUsuarioActualizacion,
        CancellationToken cancellationToken = default)
    {
        const string actualizar = """
            UPDATE USUARIO
            SET tokenActivacionHash = @tokenHash,
                tokenActivacionExpira = @tokenExpira,
                idUsuarioActualizacion = @idUsuarioActualizacion,
                fechaActualizacion = CURRENT_TIMESTAMP
            WHERE id = @id AND estado = 'PENDIENTE' AND estaActivo = 0;
            """;
        const string obtenerCorreo = """
            SELECT correo FROM USUARIO
            WHERE id = @id AND estado = 'PENDIENTE' AND estaActivo = 0
              AND tokenActivacionHash = @tokenHash
            LIMIT 1;
            """;
        await using var conexion = conexionMySql.CrearConexion();
        await conexion.OpenAsync(cancellationToken);
        await using var transaccion = await conexion.BeginTransactionAsync(cancellationToken);
        await using (var command = new MySqlCommand(actualizar, conexion, transaccion))
        {
            command.Parameters.AddWithValue("@id", usuarioId);
            command.Parameters.AddWithValue("@tokenHash", tokenHash);
            command.Parameters.AddWithValue("@tokenExpira", ZonaHorariaDeLaPaz.ALocal(tokenExpiraUtc));
            command.Parameters.AddWithValue("@idUsuarioActualizacion", idUsuarioActualizacion);
            if (await command.ExecuteNonQueryAsync(cancellationToken) != 1)
                return null;
        }

        await using var consulta = new MySqlCommand(obtenerCorreo, conexion, transaccion);
        consulta.Parameters.AddWithValue("@id", usuarioId);
        consulta.Parameters.AddWithValue("@tokenHash", tokenHash);
        var resultado = await consulta.ExecuteScalarAsync(cancellationToken);
        if (resultado is null or DBNull)
            return null;

        await transaccion.CommitAsync(cancellationToken);
        return Convert.ToString(resultado);
    }

    public async Task<bool> TokenValidoAsync(
        string tokenHash,
        DateTime ahoraUtc,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT EXISTS(
                SELECT 1 FROM USUARIO
                WHERE tokenActivacionHash = @tokenHash
                  AND tokenActivacionExpira > @ahora
                  AND estado = 'PENDIENTE'
                  AND estaActivo = 0);
            """;
        await using var conexion = conexionMySql.CrearConexion();
        await conexion.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, conexion);
        command.Parameters.AddWithValue("@tokenHash", tokenHash);
        command.Parameters.AddWithValue("@ahora", ZonaHorariaDeLaPaz.ALocal(ahoraUtc));
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 1;
    }

    public async Task<bool> ActivarAsync(
        string tokenHash,
        string passwordHash,
        DateTime ahoraUtc,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE USUARIO
            SET passwordHash = @passwordHash,
                estado = 'ACTIVO',
                estaActivo = 1,
                debeCambiarPassword = 0,
                fechaActivacion = @ahora,
                tokenActivacionHash = NULL,
                tokenActivacionExpira = NULL,
                idUsuarioActualizacion = id,
                fechaActualizacion = CURRENT_TIMESTAMP
            WHERE tokenActivacionHash = @tokenHash
              AND tokenActivacionExpira > @ahora
              AND estado = 'PENDIENTE'
              AND estaActivo = 0;
            """;
        await using var conexion = conexionMySql.CrearConexion();
        await conexion.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, conexion);
        command.Parameters.AddWithValue("@passwordHash", passwordHash);
        command.Parameters.AddWithValue("@tokenHash", tokenHash);
        command.Parameters.AddWithValue("@ahora", ZonaHorariaDeLaPaz.ALocal(ahoraUtc));
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    public async Task<bool> DesactivarAsync(
        int usuarioId,
        int idUsuarioEliminacion,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE USUARIO
            SET estado = 'INACTIVO',
                estaActivo = 0,
                idUsuarioEliminacion = @idUsuarioEliminacion,
                idUsuarioActualizacion = @idUsuarioEliminacion,
                fechaEliminacion = CURRENT_TIMESTAMP,
                fechaActualizacion = CURRENT_TIMESTAMP,
                tokenActivacionHash = NULL,
                tokenActivacionExpira = NULL
            WHERE id = @id AND estado IN ('ACTIVO', 'PENDIENTE');
            """;
        await using var conexion = conexionMySql.CrearConexion();
        await conexion.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, conexion);
        command.Parameters.AddWithValue("@id", usuarioId);
        command.Parameters.AddWithValue("@idUsuarioEliminacion", idUsuarioEliminacion);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }
}
