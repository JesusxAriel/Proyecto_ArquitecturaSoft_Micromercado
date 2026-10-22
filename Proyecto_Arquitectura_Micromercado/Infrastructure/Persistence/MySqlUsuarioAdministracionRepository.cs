using MySql.Data.MySqlClient;
using Proyecto_Arquitectura_Micromercado.Application.Users;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.Errores;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;

public sealed class MySqlUsuarioAdministracionRepository(MySqlUnidadDeTrabajo unidadDeTrabajo)
    : IUsuarioAdministracionRepository
{
    public Task<IReadOnlyList<UsuarioListadoDto>> ListarAsync(CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync<IReadOnlyList<UsuarioListadoDto>>(async () =>
        {
            const string sql = """
                SELECT u.id, COALESCE(u.nombre, u.nombreCompleto) AS nombre,
                       COALESCE(u.apellido, '') AS apellido, COALESCE(u.correo, '') AS correo,
                       u.username, r.nombre AS rol, u.estado, u.fechaCreacion
                FROM USUARIO u INNER JOIN ROL r ON r.id = u.idRol
                ORDER BY u.id;
                """;
            await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);
            await using var command = new MySqlCommand(sql, alquiler.Conexion, alquiler.Transaccion);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var usuarios = new List<UsuarioListadoDto>();
            while (await reader.ReadAsync(cancellationToken))
                usuarios.Add(new UsuarioListadoDto(reader.GetInt32(0), reader.GetString(1),
                    reader.GetString(2), reader.GetString(3), reader.GetString(4),
                    reader.GetString(5), reader.GetString(6), reader.GetDateTime(7)));
            return usuarios;
        });

    public Task<IReadOnlyList<RolActivoDto>> ListarRolesActivosAsync(CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync<IReadOnlyList<RolActivoDto>>(async () =>
        {
            const string sql = "SELECT id, nombre FROM ROL WHERE estaActivo = 1 ORDER BY nombre;";
            await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);
            await using var command = new MySqlCommand(sql, alquiler.Conexion, alquiler.Transaccion);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var roles = new List<RolActivoDto>();
            while (await reader.ReadAsync(cancellationToken))
                roles.Add(new RolActivoDto(reader.GetInt32(0), reader.GetString(1)));
            return roles;
        });

    public Task<bool> ExisteCorreoAsync(string correo, CancellationToken cancellationToken = default) =>
        ExisteAsync("SELECT EXISTS(SELECT 1 FROM USUARIO WHERE correo = @valor);", correo, cancellationToken);

    public Task<bool> ExisteUsernameAsync(string username, CancellationToken cancellationToken = default) =>
        ExisteAsync("SELECT EXISTS(SELECT 1 FROM USUARIO WHERE username = @valor);", username, cancellationToken);

    public Task<bool> EsRolActivoAsync(int idRol, CancellationToken cancellationToken = default) =>
        ExisteAsync("SELECT EXISTS(SELECT 1 FROM ROL WHERE id = @valor AND estaActivo = 1);", idRol, cancellationToken);

    private Task<bool> ExisteAsync(string sql, object valor, CancellationToken cancellationToken) =>
        ErroresDePersistencia.TraducirAsync(async () =>
        {
            await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);
            await using var command = new MySqlCommand(sql, alquiler.Conexion, alquiler.Transaccion);
            command.Parameters.AddWithValue("@valor", valor);
            return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 1;
        });

    public Task<int> CrearAsync(UsuarioAltaInterna usuario, CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync(async () =>
        {
            const string sql = """
                INSERT INTO USUARIO
                    (nombre, apellido, nombreCompleto, correo, username, idRol, passwordHash,
                     estado, estaActivo, debeCambiarPassword, idUsuarioCreacion,
                     tokenActivacionHash, tokenActivacionExpira)
                SELECT @nombre, @apellido, @nombreCompleto, @correo, @username, id, @hash,
                       'PENDIENTE', 0, 1, @creador, @tokenHash, @tokenExpira
                FROM ROL WHERE id = @rol AND estaActivo = 1;
                """;
            await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);
            await using var command = new MySqlCommand(sql, alquiler.Conexion, alquiler.Transaccion);
            command.Parameters.AddWithValue("@nombre", usuario.Nombre);
            command.Parameters.AddWithValue("@apellido", usuario.Apellido);
            command.Parameters.AddWithValue("@nombreCompleto", $"{usuario.Nombre} {usuario.Apellido}");
            command.Parameters.AddWithValue("@correo", usuario.Correo);
            command.Parameters.AddWithValue("@username", usuario.Username);
            command.Parameters.AddWithValue("@rol", usuario.IdRol);
            command.Parameters.AddWithValue("@hash", usuario.HashContrasena);
            command.Parameters.AddWithValue("@creador", usuario.IdUsuarioCreacion);
            command.Parameters.AddWithValue("@tokenHash", usuario.TokenActivacionHash);
            command.Parameters.AddWithValue("@tokenExpira", usuario.TokenActivacionExpira);
            try
            {
                if (await command.ExecuteNonQueryAsync(cancellationToken) != 1)
                    throw new ArgumentException("Debe seleccionar un rol existente y activo.");
            }
            catch (MySqlException ex) when (ex.Number == 1062 && ex.Message.Contains("UQ_Usuario_correo", StringComparison.OrdinalIgnoreCase))
            {
                throw new CorreoUsuarioDuplicadoException();
            }
            catch (MySqlException ex) when (ex.Number == 1062 && ex.Message.Contains("UQ_Usuario_username", StringComparison.OrdinalIgnoreCase))
            {
                throw new UsernameUsuarioDuplicadoException();
            }
            return checked((int)command.LastInsertedId);
        });

    public Task EliminarAsync(int usuarioId, int idUsuarioEliminacion, CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync(async () =>
        {
            const string sql = """
                UPDATE USUARIO 
                SET estado = 'INACTIVO',
                    estaActivo = 0,
                    idUsuarioEliminacion = @idUsuarioEliminacion,
                    fechaEliminacion = CURRENT_TIMESTAMP,
                    fechaActualizacion = CURRENT_TIMESTAMP
                WHERE id = @id;
                """;

            await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);
            await using var command = new MySqlCommand(sql, alquiler.Conexion, alquiler.Transaccion);
            command.Parameters.AddWithValue("@id", usuarioId);
            command.Parameters.AddWithValue("@idUsuarioEliminacion", idUsuarioEliminacion);
            await command.ExecuteNonQueryAsync(cancellationToken);
        });
}