using System.Data.Common;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using PuntoMarket.Ventas.Api.Security;
using PuntoMarket.Ventas.Application.Common;
using PuntoMarket.Ventas.Application.Customers;
using PuntoMarket.Ventas.Application.Products;
using PuntoMarket.Ventas.Application.Sales;
using PuntoMarket.Ventas.Application.Stock;
using PuntoMarket.Ventas.Domain.Stock;
using PuntoMarket.Ventas.Infrastructure.Persistence;

namespace PuntoMarket.Ventas.Tests.Api;

public sealed class VentasHttpTests : IAsyncLifetime
{
    private ApiTestHost host = null!;
    public async Task InitializeAsync() => host = await ApiTestHost.CrearAsync();
    public async Task DisposeAsync() => await host.DisposeAsync();
    private static object Solicitud(int cantidad = 2, decimal precio = 10m) =>
        new { idCliente = 3, lineas = new[] { new { idProducto = 1, cantidad, precioEsperado = precio } } };

    [Fact]
    public async Task Los_cinco_endpoints_sin_token_devuelven_401_sin_invocar_persistencia()
    {
        HttpResponseMessage[] respuestas =
        [
            await host.Client.PostAsJsonAsync("/api/ventas", Solicitud()),
            await host.Client.PostAsJsonAsync("/api/ventas/1/anular", new { motivo = "Error" }),
            await host.Client.GetAsync("/api/ventas"),
            await host.Client.GetAsync("/api/ventas/1"),
            await host.Client.GetAsync("/api/ventas/total-dia")
        ];
        foreach (var respuesta in respuestas)
        {
            using (respuesta)
            {
                Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
                Assert.Equal("Bearer", respuesta.Headers.WwwAuthenticate.Single().Scheme);
                Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
            }
        }
        Assert.Empty(host.Estado.Operaciones);
    }

    [Theory]
    [InlineData("malformado")]
    [InlineData("firma")]
    [InlineData("expirado")]
    [InlineData("issuer")]
    [InlineData("audience")]
    [InlineData("algoritmo")]
    [InlineData("sin_expiracion")]
    [InlineData("sub_cero")]
    [InlineData("sub_negativo")]
    [InlineData("sub_texto")]
    [InlineData("sin_indicador_password")]
    public async Task Un_token_que_no_cumple_el_contrato_devuelve_401(string caso)
    {
        var token = caso switch
        {
            "malformado" => "esto-no-es-un-jwt",
            "firma" => host.Token(key: Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48))),
            "expirado" => host.Token(expirado: true),
            "issuer" => host.Token(issuer: "otro-emisor"),
            "audience" => host.Token(audience: "otra-audiencia"),
            "algoritmo" => host.Token(algoritmo: Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha384),
            "sin_expiracion" => host.Token(sinExpiracion: true),
            "sub_cero" => host.Token(sub: "0"),
            "sub_negativo" => host.Token(sub: "-1"),
            "sub_texto" => host.Token(sub: "usuario"),
            _ => host.Token(sinIndicador: true)
        };
        host.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var respuesta = await host.Client.GetAsync("/api/ventas");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Empty(host.Estado.Operaciones);
    }

    [Theory]
    [InlineData("Reportes", false)]
    [InlineData("cajero", false)]
    [InlineData("Cajero", true)]
    [InlineData("Administrador", true)]
    public async Task Rol_no_permitido_o_password_pendiente_devuelve_403(string rol, bool passwordPendiente)
    {
        host.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", host.Token(rol, debeCambiar: passwordPendiente));
        using var consulta = await host.Client.GetAsync("/api/ventas");
        using var registro = await host.Client.PostAsJsonAsync("/api/ventas", Solicitud());
        Assert.Equal(HttpStatusCode.Forbidden, consulta.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, registro.StatusCode);
        Assert.Empty(host.Estado.Operaciones);
    }

    [Theory]
    [InlineData("Administrador")]
    [InlineData("Cajero")]
    public async Task El_registro_autorizado_ejecuta_la_fachada_y_audita_el_sub_del_JWT(string rol)
    {
        host.Autenticar(rol, "423");
        using var respuesta = await host.Client.PostAsJsonAsync("/api/ventas", Solicitud());
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.Equal("application/json", respuesta.Content.Headers.ContentType?.MediaType);
        var resultado = await respuesta.Content.ReadFromJsonAsync<ResultadoVentaDto>();
        Assert.NotNull(resultado);
        var cabecera = Assert.Single(host.Estado.Ventas.Cabeceras);
        Assert.Equal(423, cabecera.IdUsuario);
        Assert.Equal(20m, cabecera.MontoTotal);
        Assert.Equal(20m, resultado.Total);
        Assert.Equal(cabecera.Id, resultado.IdVenta);
        Assert.EndsWith($"/api/ventas/{cabecera.Id}", respuesta.Headers.Location!.ToString());
        Assert.Equal(2, Assert.Single(cabecera.Detalles).Cantidad);
        Assert.Contains(host.Estado.Operaciones, o => o.Operacion == "confirmar");
    }

    [Fact]
    public async Task El_cajero_no_puede_anular()
    {
        host.Autenticar();
        using var respuesta = await host.Client.PostAsJsonAsync("/api/ventas/1/anular", new { motivo = "Error" });
        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Empty(host.Estado.Operaciones);
    }

    [Fact]
    public async Task El_administrador_anula_restaura_y_audita_su_propio_identificador()
    {
        host.Autenticar("Cajero", "7");
        using var registro = await host.Client.PostAsJsonAsync("/api/ventas", Solicitud());
        Assert.Equal(HttpStatusCode.Created, registro.StatusCode);
        host.Autenticar("Administrador", "99");
        using var anulacion = await host.Client.PostAsJsonAsync("/api/ventas/1/anular", new { motivo = "  Error  " });
        Assert.Equal(HttpStatusCode.NoContent, anulacion.StatusCode);
        Assert.Equal("", await anulacion.Content.ReadAsStringAsync());
        var venta = Assert.Single(host.Estado.Ventas.Cabeceras);
        Assert.Equal(7, venta.IdUsuario);
        Assert.Equal(99, venta.IdUsuarioAnulacion);
        Assert.Equal("ANULADA", venta.Estado);
        Assert.Equal("Error", venta.Motivo);
        Assert.Equal(1000, Assert.Single(await host.Estado.Lotes.ObtenerVigentesParaDescuentoAsync(1)).CantidadDisponible);
        using var repetida = await host.Client.PostAsJsonAsync("/api/ventas/1/anular", new { motivo = "Otra vez" });
        Assert.Equal(HttpStatusCode.Conflict, repetida.StatusCode);
    }

    [Fact]
    public async Task Historial_detalle_y_total_devuelven_JSON_de_los_casos_de_uso()
    {
        host.Autenticar();
        using var registro = await host.Client.PostAsJsonAsync("/api/ventas", Solicitud());
        Assert.Equal(HttpStatusCode.Created, registro.StatusCode);
        using var historial = await host.Client.GetAsync("/api/ventas?incluirAnuladas=true");
        Assert.Equal(HttpStatusCode.OK, historial.StatusCode);
        var ventas = await historial.Content.ReadFromJsonAsync<List<VentaResumenDto>>();
        Assert.Equal(1, Assert.Single(ventas!).Id);
        Assert.True(host.Estado.IncluirAnuladas);
        Assert.Equal(50, host.Estado.Limite);
        var detalle = await host.Client.GetFromJsonAsync<VentaDetalleDto>("/api/ventas/1");
        Assert.NotNull(detalle);
        Assert.Equal(2, Assert.Single(detalle.Lineas).Cantidad);
        using var total = await host.Client.GetAsync("/api/ventas/total-dia");
        Assert.Equal(HttpStatusCode.OK, total.StatusCode);
        var json = await total.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(20m, json.GetProperty("total").GetDecimal());
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("{")]
    [InlineData("{\"idCliente\":0,\"lineas\":[{\"idProducto\":1,\"cantidad\":1}]}")]
    [InlineData("{\"idCliente\":3,\"lineas\":[]}")]
    [InlineData("{\"idCliente\":3,\"lineas\":null}")]
    [InlineData("{\"idCliente\":3,\"lineas\":[null]}")]
    [InlineData("{\"idCliente\":3,\"lineas\":[{\"idProducto\":1,\"cantidad\":0}]}")]
    [InlineData("{\"idCliente\":3,\"lineas\":[{\"idProducto\":1,\"cantidad\":-1}]}")]
    [InlineData("{\"idCliente\":3,\"lineas\":[{\"idProducto\":0,\"cantidad\":1}]}")]
    [InlineData("{\"idCliente\":3,\"idUsuario\":999,\"lineas\":[{\"idProducto\":1,\"cantidad\":1}]}")]
    public async Task Solicitudes_invalidas_y_usuario_enviado_por_cliente_se_rechazan_sin_operar(string json)
    {
        host.Autenticar();
        using var respuesta = await host.Client.PostAsync("/api/ventas", new StringContent(json, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
        Assert.Empty(host.Estado.Operaciones);
    }

    [Theory]
    [InlineData("{\"motivo\":\" \"}")]
    [InlineData("{}")]
    [InlineData("{\"motivo\":\"Error\",\"idUsuarioAnulacion\":123}")]
    public async Task La_anulacion_valida_el_cuerpo_y_rechaza_identidad_del_cliente(string json)
    {
        host.Autenticar("Administrador");
        using var respuesta = await host.Client.PostAsync("/api/ventas/1/anular", new StringContent(json, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Empty(host.Estado.Operaciones);
    }

    [Theory]
    [InlineData("/api/ventas/0", 400)]
    [InlineData("/api/ventas/-1", 400)]
    [InlineData("/api/ventas/99", 404)]
    [InlineData("/api/ventas?incluirAnuladas=no", 400)]
    public async Task Identificadores_filtros_y_detalles_inexistentes_tienen_respuesta_coherente(string ruta, int estado)
    {
        host.Autenticar();
        using var respuesta = await host.Client.GetAsync(ruta);
        Assert.Equal(estado, (int)respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task El_cambio_de_precio_se_mapea_a_conflicto_con_precio_actual()
    {
        host.Autenticar();
        using var respuesta = await host.Client.PostAsJsonAsync("/api/ventas", Solicitud(precio: 9m));
        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("precio_cambiado", problema.GetProperty("code").GetString());
        Assert.Equal(10m, problema.GetProperty("precioVigente").GetDecimal());
        Assert.Equal(1, problema.GetProperty("idProducto").GetInt32());
        Assert.Contains(host.Estado.Operaciones, o => o.Operacion == "revertir");
    }

    [Fact]
    public async Task El_stock_insuficiente_se_mapea_a_conflicto()
    {
        host.Autenticar();
        using var respuesta = await host.Client.PostAsJsonAsync("/api/ventas", Solicitud(cantidad: 1001));
        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Contains("stock_insuficiente", await respuesta.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData(99, 1)]
    [InlineData(3, 99)]
    public async Task Cliente_o_producto_no_disponible_se_mapea_a_404(int cliente, int producto)
    {
        host.Autenticar();
        using var respuesta = await host.Client.PostAsJsonAsync("/api/ventas",
            new { idCliente = cliente, lineas = new[] { new { idProducto = producto, cantidad = 1 } } });
        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Theory]
    [InlineData("restauracion", 409)]
    [InlineData("db", 503)]
    [InlineData("desconocido", 500)]
    public async Task Los_errores_tecnicos_no_exponen_detalles_internos(string tipo, int esperado)
    {
        const string secreto = "Password=secreto-no-exponer; SELECT * FROM VENTA; stack trace";
        host.Estado.Fallo = tipo switch
        {
            "restauracion" => new RestauracionExcedeCantidadInicialException(secreto),
            "db" => new ErrorDbPrueba(secreto),
            _ => new InvalidOperationException(secreto)
        };
        host.Autenticar();
        using var respuesta = await host.Client.GetAsync("/api/ventas");
        Assert.Equal(esperado, (int)respuesta.StatusCode);
        var body = await respuesta.Content.ReadAsStringAsync();
        Assert.DoesNotContain(secreto, body);
        Assert.DoesNotContain("SELECT", body);
        Assert.DoesNotContain("Password", body);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Dos_solicitudes_usan_sesiones_distintas_y_cada_una_comparten_sus_puertos()
    {
        host.Autenticar();
        var respuestas = await Task.WhenAll(host.Client.PostAsJsonAsync("/api/ventas", Solicitud()),
            host.Client.PostAsJsonAsync("/api/ventas", Solicitud()));
        foreach (var respuesta in respuestas) { Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode); respuesta.Dispose(); }
        var grupos = host.Estado.Operaciones.GroupBy(o => o.Sesion).ToArray();
        Assert.Equal(2, grupos.Length);
        foreach (var grupo in grupos)
        {
            var operaciones = grupo.Select(o => o.Operacion).ToArray();
            Assert.Contains("iniciar", operaciones);
            Assert.Contains("cabecera", operaciones);
            Assert.Contains("precio", operaciones);
            Assert.Contains("descontar", operaciones);
            Assert.Contains("confirmar", operaciones);
        }
        Assert.Equal(2, host.Estado.Disposiciones.Count);
        await Task.WhenAll(host.Estado.Disposiciones.Select(d => d.Task)).WaitAsync(TimeSpan.FromSeconds(10));
    }

    private sealed class ErrorDbPrueba(string message) : DbException(message);
}

public class ComposicionApiTests
{
    [Fact]
    public async Task La_salud_arranca_sin_JWT_ni_configuracion_o_conexion_MySQL()
    {
        await using var host = await ApiTestHost.CrearAsync(puertosDePrueba: false, jwtConfigurado: false);
        using var salud = await host.Client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, salud.StatusCode);
        var json = await salud.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ok", json.GetProperty("status").GetString());
        host.Autenticar();
        using var protegida = await host.Client.GetAsync("/api/ventas");
        Assert.Equal(HttpStatusCode.Unauthorized, protegida.StatusCode);
    }

    [Fact]
    public async Task Una_solicitud_autorizada_sin_configuracion_MySQL_responde_503_seguro()
    {
        await using var host = await ApiTestHost.CrearAsync(puertosDePrueba: false);
        host.Autenticar();
        using var respuesta = await host.Client.GetAsync("/api/ventas");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, respuesta.StatusCode);
        Assert.Contains("configuracion_pendiente", await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task La_composicion_real_comparte_unidad_por_scope_y_la_dispone_sin_abrir_conexion()
    {
        await using var host = await ApiTestHost.CrearAsync(puertosDePrueba: false);
        host.App.Configuration["ConnectionStrings:Ventas"] = "Server=localhost;Database=ventas_composicion_test;";
        MySqlUnidadDeTrabajo primera;
        await using (var scope = host.App.Services.CreateAsyncScope())
        {
            var sesion = scope.ServiceProvider.GetRequiredService<PersistenciaVentasMySql>();
            primera = sesion.Unidad;
            Assert.Same(primera, scope.ServiceProvider.GetRequiredService<IUnidadDeTrabajo>());
            Assert.Same(sesion.Ventas, scope.ServiceProvider.GetRequiredService<IVentaRepository>());
            Assert.Same(sesion.Lotes, scope.ServiceProvider.GetRequiredService<ILoteRepository>());
            Assert.Same(sesion.Precios, scope.ServiceProvider.GetRequiredService<IConPrecioVigente>());
            Assert.Same(sesion.Clientes, scope.ServiceProvider.GetRequiredService<IClienteActivoConsulta>());
            Assert.Same(sesion.Consultas, scope.ServiceProvider.GetRequiredService<IVentaConsultaRepository>());
            Assert.False(primera.HayTransaccionActiva);
        }
        await Assert.ThrowsAsync<ObjectDisposedException>(() => primera.IniciarAsync());
        await using var otro = host.App.Services.CreateAsyncScope();
        Assert.NotSame(primera, otro.ServiceProvider.GetRequiredService<IUnidadDeTrabajo>());
    }

    [Fact]
    public void El_adaptador_de_usuario_usa_claims_sin_mapeos_implicitos()
    {
        var identidad = new ClaimsIdentity([
            new("sub", "42"), new("unique_name", "cajero.real"), new("nombre", "Nombre Mostrado"),
            new("role", "Cajero")], "Bearer", "unique_name", "role");
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identidad) } };
        var usuario = new UsuarioActualDesdeJwt(accessor);
        Assert.Equal(42, usuario.Id);
        Assert.Equal("cajero.real", usuario.NombreUsuario);
        Assert.Equal("Cajero", usuario.Rol);
        accessor.HttpContext = null;
        Assert.Throws<InvalidOperationException>(() => usuario.Id);
    }
}
