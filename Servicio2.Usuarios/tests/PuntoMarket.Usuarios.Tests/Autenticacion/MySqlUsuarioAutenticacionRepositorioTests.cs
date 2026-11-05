using MySql.Data.MySqlClient;
using PuntoMarket.Usuarios.Application.Ports.Autenticacion;
using PuntoMarket.Usuarios.Domain.Enums;
using PuntoMarket.Usuarios.Infrastructure.Persistence;
using Xunit;

namespace PuntoMarket.Usuarios.Tests.Autenticacion;

public class MySqlUsuarioAutenticacionRepositorioTests
{
    [MySqlAutenticacionFact]
    [Trait("Categoria", "MySql")]
    public async Task Obtiene_credenciales_y_datos_del_rol_desde_el_esquema()
    {
        var cadenaConexion = Environment.GetEnvironmentVariable("MICROMERCADO_PRUEBAS_MYSQL")!;
        await using var conexion = new MySqlConnection(cadenaConexion);
        await conexion.OpenAsync();

        const string sql = """
            SELECT u.correo, u.id, u.username, u.nombreCompleto, u.estado,
                   u.debeCambiarPassword, r.nombre, r.estaActivo
            FROM USUARIO u
            INNER JOIN ROL r ON r.id = u.idRol
            WHERE u.correo IS NOT NULL
            ORDER BY u.id
            LIMIT 1;
            """;
        string correo;
        int id;
        string username;
        string nombreCompleto;
        EstadoUsuario estado;
        bool debeCambiarPassword;
        string rol;
        bool rolActivo;
        await using (var command = new MySqlCommand(sql, conexion))
        await using (var reader = await command.ExecuteReaderAsync())
        {
            Assert.True(await reader.ReadAsync(), "La base de pruebas debe contener un usuario con correo.");
            correo = reader.GetString(0);
            id = reader.GetInt32(1);
            username = reader.GetString(2);
            nombreCompleto = reader.GetString(3);
            estado = Enum.Parse<EstadoUsuario>(reader.GetString(4));
            debeCambiarPassword = reader.GetBoolean(5);
            rol = reader.GetString(6);
            rolActivo = reader.GetBoolean(7);
        }

        var repositorio = new MySqlUsuarioAutenticacionRepositorio(new ConexionMySql(cadenaConexion));

        var usuario = await repositorio.ObtenerPorCorreoAsync(correo);

        Assert.NotNull(usuario);
        Assert.Equal(id, usuario.Id);
        Assert.Equal(username, usuario.Username);
        Assert.False(string.IsNullOrWhiteSpace(usuario.PasswordHash));
        Assert.Equal(nombreCompleto, usuario.NombreCompleto);
        Assert.Equal(estado, usuario.Estado);
        Assert.Equal(debeCambiarPassword, usuario.DebeCambiarPassword);
        Assert.Equal(rol, usuario.Rol);
        Assert.Equal(rolActivo, usuario.RolActivo);
    }

    [MySqlAutenticacionFact]
    [Trait("Categoria", "MySql")]
    public async Task Devuelve_null_cuando_no_existe_el_correo()
    {
        var cadenaConexion = Environment.GetEnvironmentVariable("MICROMERCADO_PRUEBAS_MYSQL")!;
        var repositorio = new MySqlUsuarioAutenticacionRepositorio(new ConexionMySql(cadenaConexion));

        var usuario = await repositorio.ObtenerPorCorreoAsync(
            $"no-existe-{Guid.NewGuid():N}@example.invalid");

        Assert.Null(usuario);
    }

    [Fact]
    public async Task Propaga_cancelacion_al_abrir_la_conexion()
    {
        var conexion = new ConexionMySql(
            "Server=localhost;Database=pruebas;Uid=pruebas;Pwd=pruebas;");
        var repositorio = new MySqlUsuarioAutenticacionRepositorio(conexion);
        using var origen = new CancellationTokenSource();
        await origen.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repositorio.ObtenerPorCorreoAsync("ana@example.com", origen.Token));
    }
}

public sealed class MySqlAutenticacionFactAttribute : FactAttribute
{
    public MySqlAutenticacionFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MICROMERCADO_PRUEBAS_MYSQL")))
            Skip = "Falta MICROMERCADO_PRUEBAS_MYSQL con el esquema de pruebas actualizado.";
    }
}
