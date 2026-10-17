using System.Reflection;
using MySql.Data.MySqlClient;
using Proyecto_Arquitectura_Micromercado.Application.Users;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Database;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Security;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class UsuarioAdministracionMySqlTests
{
    // Base de pruebas previamente creada con bdMicroMercadoArqui.sql actualizado.
    // Todos los datos creados aquí se revierten al terminar.
    [MySqlUsuariosFact]
    [Trait("Categoria", "MySql")]
    public async Task Alta_listado_restricciones_y_roles_funcionan_en_mysql()
    {
        var cadena = Environment.GetEnvironmentVariable("MICROMERCADO_PRUEBAS_MYSQL")!;
        var fabrica = (DatabaseConnection)typeof(DatabaseConnection)
            .GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, [typeof(string)], null)!
            .Invoke([cadena]);
        await using var unidad = new MySqlUnidadDeTrabajo(fabrica);
        await unidad.IniciarAsync();
        var conexion = (MySqlConnection)typeof(MySqlUnidadDeTrabajo)
            .GetField("conexionDeTransaccion", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(unidad)!;
        var transaccion = (MySqlTransaction)typeof(MySqlUnidadDeTrabajo)
            .GetField("transaccion", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(unidad)!;
        async Task<object?> Escalar(string sql, params (string, object)[] parametros)
        {
            await using var command = new MySqlCommand(sql, conexion, transaccion);
            foreach (var (nombre, valor) in parametros) command.Parameters.AddWithValue(nombre, valor);
            return await command.ExecuteScalarAsync();
        }

        var repo = new MySqlUsuarioAdministracionRepository(unidad);
        var roles = await repo.ListarRolesActivosAsync();
        Assert.NotEmpty(roles);
        var creador = Convert.ToInt32(await Escalar("SELECT id FROM USUARIO WHERE estaActivo = 1 ORDER BY id LIMIT 1;"));
        Assert.True(creador > 0);
        var identificador = Guid.NewGuid().ToString("N");
        await Escalar("INSERT INTO ROL(nombre, estaActivo) VALUES (@nombre, 0);", ("@nombre", identificador));
        var inactivo = Convert.ToInt32(await Escalar("SELECT id FROM ROL WHERE nombre = @nombre;", ("@nombre", identificador)));
        Assert.False(await repo.EsRolActivoAsync(inactivo));
        Assert.DoesNotContain(await repo.ListarRolesActivosAsync(), r => r.Id == inactivo);

        var alta = new UsuarioAltaInterna("Ariana", "Pérez", identificador + "@ejemplo.com",
            identificador, roles[0].Id, new BCryptPasswordHasher().Hash(Guid.NewGuid().ToString()), creador);
        var id = await repo.CrearAsync(alta);
        Assert.True(await repo.ExisteCorreoAsync(alta.Correo.ToUpperInvariant()));
        Assert.True(await repo.ExisteUsernameAsync(alta.Username));
        var listado = Assert.Single(await repo.ListarAsync(), u => u.Id == id);
        Assert.Equal("PENDIENTE", listado.Estado);
        Assert.Equal("Ariana", listado.Nombre);
        Assert.Equal("Pérez", listado.Apellido);
        Assert.Equal(alta.Correo, listado.Correo);
        Assert.True(listado.FechaCreacion > DateTime.MinValue);
        Assert.Equal(0, Convert.ToInt32(await Escalar("SELECT estaActivo FROM USUARIO WHERE id = @id;", ("@id", id))));
        Assert.Equal(1, Convert.ToInt32(await Escalar("SELECT debeCambiarPassword FROM USUARIO WHERE id = @id;", ("@id", id))));
        Assert.Equal(creador, Convert.ToInt32(await Escalar("SELECT idUsuarioCreacion FROM USUARIO WHERE id = @id;", ("@id", id))));
        await Assert.ThrowsAsync<CorreoUsuarioDuplicadoException>(() => repo.CrearAsync(alta with { Username = identificador + "1" }));
        await Assert.ThrowsAsync<UsernameUsuarioDuplicadoException>(() => repo.CrearAsync(alta with { Correo = "otra" + alta.Correo }));
        await Assert.ThrowsAsync<ArgumentException>(() => repo.CrearAsync(alta with { IdRol = inactivo }));
        await Assert.ThrowsAsync<ArgumentException>(() => repo.CrearAsync(alta with { IdRol = int.MaxValue }));
        await unidad.RevertirAsync();
    }
}

public sealed class MySqlUsuariosFactAttribute : FactAttribute
{
    public MySqlUsuariosFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MICROMERCADO_PRUEBAS_MYSQL")))
            Skip = "Falta MICROMERCADO_PRUEBAS_MYSQL con el esquema de pruebas actualizado.";
    }
}
