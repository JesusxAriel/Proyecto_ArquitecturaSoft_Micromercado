using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PuntoMarket.Ventas.Frontend;
using PuntoMarket.Ventas.Frontend.Clients;
using PuntoMarket.Ventas.Frontend.Security;

namespace PuntoMarket.Ventas.Tests.Frontend;

public sealed class ConsultasRazorTests
{
    private static readonly string Clave = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48));
    [Fact]
    public async Task Usuarios_admin_y_cajero_aislan_cookie_JWT_permisos_y_logout()
    {
        var roles = new List<(string Rol, string Id, string Usuario)>();
        await using var host = await Host.Crear(adminEnabled: true, alSolicitar: request =>
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(request.Headers.Authorization!.Parameter);
            roles.Add((jwt.Claims.Single(c => c.Type == "role").Value,
                jwt.Subject, jwt.Claims.Single(c => c.Type == "unique_name").Value));
        });
        using var cajero = host.Cliente();
        using var admin = host.Cliente();
        await Login(cajero);
        await Login(admin, administrador: true);
        var paginaAdmin = await admin.GetStringAsync("/Sales/Historial");
        Assert.Contains("anularVentaModal", paginaAdmin);
        Assert.Contains("admin · Administrador · Usuarios", paginaAdmin);
        var paginaCajero = await cajero.GetStringAsync("/Sales/Historial");
        Assert.DoesNotContain("anularVentaModal", paginaCajero);
        Assert.Contains("cajero1 · Cajero · Usuarios", paginaCajero);
        foreach (var html in new[] { paginaAdmin, paginaCajero })
        {
            Assert.DoesNotContain("Bearer ", html);
            Assert.DoesNotContain("eyJhbGci", html);
        }
        using var solicitud = new HttpRequestMessage(HttpMethod.Post, "/Sales/Historial?handler=AnularVenta")
        { Content = new StringContent("{\"idVenta\":2,\"motivo\":\"  Error  \"}", Encoding.UTF8, "application/json") };
        solicitud.Headers.Add("RequestVerificationToken", Csrf(paginaAdmin));
        using var respuesta = await admin.SendAsync(solicitud);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("\"exito\":true", await respuesta.Content.ReadAsStringAsync());
        await cajero.GetAsync("/Sales/Historial?handler=Historial");
        Assert.Contains(("Administrador", "1", "admin"), roles);
        Assert.Contains(("Cajero", "2", "cajero1"), roles);
        var salir = new FormUrlEncodedContent(new Dictionary<string,string> { ["__RequestVerificationToken"] = Csrf(paginaAdmin) });
        await admin.PostAsync("/Account/Logout", salir);
        Assert.Equal(HttpStatusCode.Unauthorized, (await admin.GetAsync("/Sales/Historial?handler=Historial")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cajero.GetAsync("/Sales/Historial?handler=Historial")).StatusCode);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{")]
    [InlineData("{\"idVenta\":\"invalido\",\"motivo\":\"Error\"}")]
    [InlineData("{\"idVenta\":0,\"motivo\":\"Error\"}")]
    [InlineData("{\"idVenta\":2,\"motivo\":\"   \"}")]
    [InlineData("largo")]
    public async Task Administrador_rechaza_anulacion_invalida_sin_invocar_API(string json)
    {
        var llamadas = 0;
        await using var host = await Host.Crear(adminEnabled: true, alEnviar: () => llamadas++);
        using var cliente = host.Cliente();
        await Login(cliente, administrador: true);
        var html = await cliente.GetStringAsync("/Sales/Historial");
        if (json == "largo") json = "{\"idVenta\":2,\"motivo\":\"" + new string('a', 256) + "\"}";
        using var request = new HttpRequestMessage(HttpMethod.Post, "/Sales/Historial?handler=AnularVenta")
        { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        request.Headers.Add("RequestVerificationToken", Csrf(html));
        using var respuesta = await cliente.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal(0, llamadas);
    }

    [Fact]
    public async Task Login_y_anulacion_exigen_antiforgery()
    {
        var llamadas = 0;
        await using var host = await Host.Crear(adminEnabled: true, alEnviar: () => llamadas++);
        using var cliente = host.Cliente();
        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsync("/Account/Login", new StringContent(""))).StatusCode);
        await Login(cliente, administrador: true);
        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsync("/Sales/Historial?handler=AnularVenta",
            new StringContent("{\"idVenta\":2,\"motivo\":\"Error\"}", Encoding.UTF8, "application/json"))).StatusCode);
        Assert.Equal(0, llamadas);
    }

    [Fact]
    public async Task Demo_no_permite_seleccionar_rol_incluso_con_campos_inyectados()
    {
        await using var host=await Host.Crear(); using var c=host.Cliente();
        var html=await c.GetStringAsync("/Account/Login");
        var r=await c.PostAsync("/Demo?handler=Administrador",new FormUrlEncodedContent(new Dictionary<string,string>{{"__RequestVerificationToken",Csrf(html)},{"role","Administrador"},{"id","1"}}));
        Assert.Equal(HttpStatusCode.Redirect,r.StatusCode); Assert.Equal("/Account/Login",r.Headers.Location!.OriginalString);
        Assert.Equal(HttpStatusCode.Unauthorized,(await c.GetAsync("/Sales?handler=Stock&id=1")).StatusCode);
    }

    [Theory]
    [InlineData("Production",true)] [InlineData("Development",false)]
    public async Task Flags_Demo_no_habilitan_acceso_por_perfiles(string ambiente,bool demoEnabled)
    {
        await using var host=await Host.Crear(ambiente,adminEnabled:true,demoEnabled:demoEnabled); using var c=host.Cliente();
        var r=await c.GetAsync("/Demo"); Assert.Equal(HttpStatusCode.Redirect,r.StatusCode); Assert.Equal("/Account/Login",r.Headers.Location!.OriginalString);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("")]
    [InlineData("{")]
    [InlineData("{}")]
    [InlineData("{\"idCliente\":3,\"lineas\":null}")]
    [InlineData("{\"idCliente\":3,\"lineas\":[null]}")]
    [InlineData("{\"idCliente\":3,\"lineas\":[]}")]
    [InlineData("{\"idCliente\":3,\"lineas\":[{\"idProducto\":1,\"cantidad\":\"incorrecta\",\"precioEsperado\":10}]}")]
    [InlineData("{\"idCliente\":3,\"lineas\":[{\"idProducto\":1,\"cantidad\":0,\"precioEsperado\":10}]}")]
    [InlineData("{\"idCliente\":3,\"lineas\":[{\"idProducto\":1,\"cantidad\":1,\"precioEsperado\":-1}]}")]
    public async Task Registro_Razor_rechaza_cuerpos_invalidos_sin_invocar_API(string json)
    {
        var llamadas = 0;
        await using var host = await Host.Crear(alEnviar: () => llamadas++);
        using var cliente = host.Cliente();
        await Login(cliente);
        var csrf = Csrf(await cliente.GetStringAsync("/Sales"));
        using var request = new HttpRequestMessage(HttpMethod.Post, "/Sales?handler=RegistrarVenta")
        { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        request.Headers.Add("RequestVerificationToken", csrf);
        using var respuesta = await cliente.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Contains("Revise el cliente", await respuesta.Content.ReadAsStringAsync());
        Assert.Equal(0, llamadas);
    }

    [Fact]
    public async Task Registro_Razor_valido_con_antiforgery_devuelve_identificador_y_total()
    {
        var llamadas = 0;
        await using var host = await Host.Crear(alEnviar: () => llamadas++);
        using var cliente = host.Cliente();
        await Login(cliente);
        var csrf = Csrf(await cliente.GetStringAsync("/Sales"));
        using var request = new HttpRequestMessage(HttpMethod.Post, "/Sales?handler=RegistrarVenta")
        { Content = new StringContent("{\"idCliente\":3,\"lineas\":[{\"idProducto\":1,\"cantidad\":2,\"precioEsperado\":10}]}", Encoding.UTF8, "application/json") };
        request.Headers.Add("RequestVerificationToken", csrf);
        using var respuesta = await cliente.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var json = await respuesta.Content.ReadAsStringAsync();
        Assert.Contains("\"idVenta\":42", json);
        Assert.Contains("\"total\":20", json);
        Assert.DoesNotContain("Bearer ", json);
        Assert.Equal(1, llamadas);
    }

    [Theory]
    [InlineData("Cajero", HttpStatusCode.OK)]
    [InlineData("Administrador", HttpStatusCode.OK)]
    [InlineData("Reportes", HttpStatusCode.Redirect)]
    public async Task Comprobante_aplica_roles_sin_exponer_JWT(string rol, HttpStatusCode esperado)
    {
        await using var host = await Host.Crear(rolDePrueba: rol);
        using var cliente = host.Cliente();
        await cliente.GetAsync("/sesion-prueba");
        using var respuesta = await cliente.GetAsync("/Comprobante/2");
        Assert.Equal(esperado, respuesta.StatusCode);
        var html = await respuesta.Content.ReadAsStringAsync();
        if (esperado == HttpStatusCode.OK) Assert.Contains("V-20261104-0002", html);
        Assert.DoesNotContain("Bearer ", html);
        Assert.DoesNotContain("eyJhbGci", html);
    }
    [Theory]
    [InlineData(401)] [InlineData(403)] [InlineData(404)] [InlineData(409)] [InlineData(503)] [InlineData(500)]
    public async Task Cliente_mapea_errores_sin_devolver_cuerpo_privado(int codigo)
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sesiones = SesionRealPrueba.Sesiones(cache,Clave);
        var id = sesiones.Crear(SesionRealPrueba.Token(Clave));
        var contexto = SesionRealPrueba.Contexto(sesiones,id);
        using var http = new HttpClient(new RespuestaHandler(codigo)) { BaseAddress = new Uri("http://localhost:5002/") };
        var cliente = new VentasApiClient(http, new ResolucionSesion(new HttpContextAccessor { HttpContext = contexto }, sesiones));
        var error = await Assert.ThrowsAsync<ConsultaApiException>(() => cliente.Precio(1, default));
        Assert.Equal(codigo == 500 ? 503 : codigo, error.Estado);
        Assert.DoesNotContain("privado", error.Mensaje);
    }
    [Fact]
    public void Sesiones_aislan_tokens_y_logout_no_afecta_a_otra_sesion()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sesiones = SesionRealPrueba.Sesiones(cache,Clave);
        var a = sesiones.Crear(SesionRealPrueba.Token(Clave)); var b = sesiones.Crear(SesionRealPrueba.Token(Clave));
        Assert.True(a != b);
        Assert.NotNull(sesiones.Token(a)); Assert.NotNull(sesiones.Token(b));
        sesiones.Cerrar(a);
        Assert.Null(sesiones.Token(a)); Assert.NotNull(sesiones.Token(b));
    }
    [Fact]
    public void Tokens_fabricados_no_crean_sesion()
    {
        using var cache=new MemoryCache(new MemoryCacheOptions());
        Assert.Throws<TokenUsuariosInvalidoException>(()=>SesionRealPrueba.Sesiones(cache,Clave).Crear("token-no-valido"));
    }

    [Fact]
    public void Token_no_se_renueva_y_la_sesion_expira_con_el_JWT()
    {
        using var cache=new MemoryCache(new MemoryCacheOptions()); var reloj=new RelojMovible();
        var sesiones=SesionRealPrueba.Sesiones(cache,Clave,reloj); var token=SesionRealPrueba.Token(Clave,reloj:reloj); var id=sesiones.Crear(token);
        reloj.Avanzar(TimeSpan.FromMinutes(4)); Assert.Equal(token,sesiones.Token(id));
        reloj.Avanzar(TimeSpan.FromMinutes(2)); Assert.Null(sesiones.Token(id));
    }

    [Fact]
    public async Task Razor_sesion_antiforgery_consultas_y_escrituras_bloqueadas()
    {
        await using var host = await Host.Crear();
        using var anonimo = host.Cliente();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync("/Sales?handler=BuscarProductos&termino=leche")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await anonimo.PostAsync("/Demo", new StringContent(""))).StatusCode);
        using var cliente = host.Cliente();
        await Login(cliente);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync("/Sales?handler=BuscarClientes&termino=JUAN")).StatusCode);
        var html = await cliente.GetStringAsync("/Sales");
        Assert.DoesNotContain("Bearer ", html);
        Assert.DoesNotContain("eyJhbGci", html);
        Assert.Contains("Confirmar venta", html);
        var respuesta = await cliente.GetAsync("/Sales?handler=BuscarProductos&termino=leche");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("productos", await respuesta.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.GetAsync("/Sales?handler=BuscarProductos&termino=" + new string('a',151))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsync("/Sales?handler=RegistrarVenta",
            new StringContent("{}", Encoding.UTF8, "application/json"))).StatusCode);
        var csrf = Csrf(await cliente.GetStringAsync("/Sales"));
        using var clienteSolicitud = new HttpRequestMessage(HttpMethod.Post, "/Sales?handler=CrearCliente")
        {
            Content = new StringContent("{\"nitCi\":\"12345\",\"razonSocial\":\"Cliente Nuevo\"}", Encoding.UTF8, "application/json")
        };
        clienteSolicitud.Headers.Add("RequestVerificationToken", csrf);
        Assert.Equal(HttpStatusCode.OK, (await cliente.SendAsync(clienteSolicitud)).StatusCode);
        var salir = new FormUrlEncodedContent(new Dictionary<string,string> { ["__RequestVerificationToken"] = Csrf(await cliente.GetStringAsync("/Sales")) });
        await cliente.PostAsync("/Account/Logout", salir);
        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/Sales?handler=Stock&id=1")).StatusCode);
    }
    [Fact]
    public async Task Cliente_registra_venta_con_jwt_solo_en_servidor()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sesiones = SesionRealPrueba.Sesiones(cache,Clave);
        var id = sesiones.Crear(SesionRealPrueba.Token(Clave));
        var contexto = SesionRealPrueba.Contexto(sesiones,id);
        using var http = new HttpClient(new RespuestaHandler()) { BaseAddress = new Uri("http://localhost:5002/") };
        var cliente = new VentasApiClient(http, new ResolucionSesion(new HttpContextAccessor { HttpContext = contexto }, sesiones));

        var resultado = await cliente.RegistrarVenta(3, [new LineaVentaFrontend(1, 2, 10m)], default);

        Assert.Equal(42, resultado.IdVenta);
        Assert.Equal(20m, resultado.Total);
    }
    [Fact]
    public async Task Cliente_crea_cliente_con_jwt_solo_en_servidor()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sesiones = SesionRealPrueba.Sesiones(cache,Clave);
        var id = sesiones.Crear(SesionRealPrueba.Token(Clave));
        var contexto = SesionRealPrueba.Contexto(sesiones,id);
        using var http = new HttpClient(new RespuestaHandler()) { BaseAddress = new Uri("http://localhost:5002/") };
        var cliente = new VentasApiClient(http, new ResolucionSesion(new HttpContextAccessor { HttpContext = contexto }, sesiones));

        var creado = await cliente.CrearCliente("12345", "Cliente Nuevo", default);

        Assert.Equal(11, creado.Id);
        Assert.DoesNotContain("Bearer ", creado.RazonSocial);
    }
    [Fact]
    public async Task Cliente_consulta_historial_y_detalle_con_jwt_solo_en_servidor()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sesiones = SesionRealPrueba.Sesiones(cache,Clave);
        var id = sesiones.Crear(SesionRealPrueba.Token(Clave));
        var contexto = SesionRealPrueba.Contexto(sesiones,id);
        using var http = new HttpClient(new RespuestaHandler()) { BaseAddress = new Uri("http://localhost:5002/") };
        var cliente = new VentasApiClient(http, new ResolucionSesion(new HttpContextAccessor { HttpContext = contexto }, sesiones));

        var ventas = await cliente.Historial(false, default);
        var detalle = await cliente.Detalle(2, default);

        Assert.Single(ventas);
        Assert.Equal("V-20261104-0002", ventas[0].NroComprobante);
        Assert.Equal("Juan", ventas[0].NombreCliente);
        Assert.Equal(360m, ventas[0].Total);
        Assert.Equal(2, detalle.Id);
        Assert.Equal("COMPLETADA", detalle.Estado);
        Assert.Single(detalle.Lineas);
    }
    [Fact]
    public async Task Cliente_consulta_el_html_del_comprobante_por_el_id_solicitado()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sesiones = SesionRealPrueba.Sesiones(cache,Clave);
        var id = sesiones.Crear(SesionRealPrueba.Token(Clave));
        var contexto = SesionRealPrueba.Contexto(sesiones,id);
        using var http = new HttpClient(new RespuestaHandler()) { BaseAddress = new Uri("http://localhost:5002/") };
        var cliente = new VentasApiClient(http, new ResolucionSesion(new HttpContextAccessor { HttpContext = contexto }, sesiones));

        var html = await cliente.ComprobanteHtml(2, default);
        var otroHtml = await cliente.ComprobanteHtml(1, default);

        Assert.Contains("V-20261104-0002", html);
        Assert.Contains("360,00", html);
        Assert.Contains("V-20261104-0001", otroHtml);
        Assert.Contains("95,50", otroHtml);
        Assert.DoesNotContain("V-20261104-0002", otroHtml);
        Assert.DoesNotContain("Bearer ", html);
    }
    [Fact]
    public async Task Cliente_anula_una_venta_con_jwt_solo_en_servidor()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sesiones = SesionRealPrueba.Sesiones(cache,Clave);
        var id = sesiones.Crear(SesionRealPrueba.Token(Clave));
        var contexto = SesionRealPrueba.Contexto(sesiones,id);
        using var http = new HttpClient(new RespuestaHandler()) { BaseAddress = new Uri("http://localhost:5002/") };
        var cliente = new VentasApiClient(http, new ResolucionSesion(new HttpContextAccessor { HttpContext = contexto }, sesiones));

        await cliente.AnularVenta(2, "Corrección", default);
    }
    [Fact]
    public async Task Cajero_no_puede_anular_desde_el_handler_Razor()
    {
        await using var host = await Host.Crear();
        using var cliente = host.Cliente();
        await Login(cliente);
        var csrf = Csrf(await cliente.GetStringAsync("/Sales/Historial"));
        using var solicitud = new StringContent("{\"idVenta\":2,\"motivo\":\"Corrección\"}", Encoding.UTF8, "application/json");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/Sales/Historial?handler=AnularVenta") { Content = solicitud };
        request.Headers.Add("RequestVerificationToken", csrf);
        using var respuesta = await cliente.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Contains("No tiene permiso", await respuesta.Content.ReadAsStringAsync());
    }
    [Fact]
    public async Task Pagina_de_comprobante_requiere_sesion_y_muestra_el_html_de_la_venta()
    {
        await using var host = await Host.Crear();
        using var anonimo = host.Cliente();
        var acceso = await anonimo.GetAsync("/Comprobante/2");
        Assert.Equal(HttpStatusCode.Redirect, acceso.StatusCode);
        Assert.Contains("/Account/Login", acceso.Headers.Location?.OriginalString);

        using var cliente = host.Cliente();
        await Login(cliente);
        using var respuesta = await cliente.GetAsync("/Comprobante/2");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var html = await respuesta.Content.ReadAsStringAsync();
        Assert.Contains("V-20261104-0002", html);
        Assert.Contains("360,00", html);
        Assert.Contains("Imprimir Comprobante", html);
    }
    [Fact]
    public async Task Produccion_redirige_Demo_a_login_real()
    {
        await using var host=await Host.Crear("Production");using var c=host.Cliente();
        var r=await c.GetAsync("/Demo");Assert.Equal(HttpStatusCode.Redirect,r.StatusCode);Assert.Equal("/Account/Login",r.Headers.Location!.OriginalString);
    }
    private static string Csrf(string html) => WebUtility.HtmlDecode(Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
    private static async Task Login(HttpClient cliente, bool administrador = false)
    {
        var html=await cliente.GetStringAsync("/Account/Login");
        using var formulario=new FormUrlEncodedContent(new Dictionary<string,string>{{"__RequestVerificationToken",Csrf(html)},{"Input.Correo",administrador?"admin@example.invalid":"cajero@example.invalid"},{"Input.Contrasena","FixtureNoReal123!"}});
        using var respuesta=await cliente.PostAsync("/Account/Login",formulario);
        Assert.Equal(HttpStatusCode.Redirect,respuesta.StatusCode);
        Assert.Contains(respuesta.Headers.GetValues("Set-Cookie"),c=>c.StartsWith(AccesoUsuariosPreparado.Cookie+"=")&&c.Contains("httponly",StringComparison.OrdinalIgnoreCase));
    }
    private sealed class RelojMovible : TimeProvider
    {
        private DateTimeOffset ahora = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => ahora;
        public void Avanzar(TimeSpan intervalo) => ahora += intervalo;
    }
    private sealed class RespuestaHandler(int codigo = 200, Action? alEnviar = null, Action<HttpRequestMessage>? alSolicitar = null) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            alEnviar?.Invoke();
            Assert.True(request.Headers.Authorization?.Scheme == "Bearer");
            alSolicitar?.Invoke(request);
            if (request.Method == HttpMethod.Post && request.RequestUri?.AbsolutePath.EndsWith("/anular", StringComparison.Ordinal) == true)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
            }
            if (request.Method == HttpMethod.Post)
            {
                if (request.RequestUri?.AbsolutePath == "/api/clientes")
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created)
                    {
                        Content = new StringContent("{\"id\":11,\"nitCi\":\"12345\",\"razonSocial\":\"Cliente Nuevo\"}", Encoding.UTF8, "application/json")
                    });
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent("{\"idVenta\":42,\"total\":20}", Encoding.UTF8, "application/json")
                });
            }
            Assert.Equal(HttpMethod.Get, request.Method);
            // Datos de fixture solo para pruebas aisladas, nunca para el frontend real.
            var contenido = request.RequestUri?.AbsolutePath switch
            {
                "/api/ventas" => "[{\"id\":2,\"nroComprobante\":\"V-20261104-0002\",\"fechaHora\":\"2026-11-04T17:17:22\",\"nombreCliente\":\"Juan\",\"estado\":\"COMPLETADA\",\"total\":360}]",
                "/api/ventas/2" => "{\"id\":2,\"nroComprobante\":\"V-20261104-0002\",\"fechaHora\":\"2026-11-04T17:17:22\",\"nitCiCliente\":\"4545451226\",\"razonSocialCliente\":\"Juan\",\"usuarioCajero\":\"cajero1\",\"estado\":\"COMPLETADA\",\"total\":360,\"lineas\":[{\"cantidad\":80,\"descripcion\":\"Galletas\",\"precioUnitario\":4.5,\"subtotal\":360,\"lotes\":[{\"codigoLote\":\"LOT-MOK-A\",\"cantidad\":80}]}]}",
                "/api/ventas/2/comprobante/html" => "<!DOCTYPE html><html><body><h1>COMPROBANTE DE VENTA</h1><p>V-20261104-0002</p><p>360,00</p><button>Imprimir Comprobante</button></body></html>",
                "/api/ventas/1/comprobante/html" => "<!DOCTYPE html><html><body><h1>COMPROBANTE DE VENTA</h1><p>V-20261104-0001</p><p>95,50</p><button>Imprimir Comprobante</button></body></html>",
                _ => "{\"productos\":[],\"hayMas\":false,\"total\":0}"
            };
            return Task.FromResult(new HttpResponseMessage((HttpStatusCode)codigo) { Content = new StringContent(codigo == 200 ? contenido : "privado", Encoding.UTF8, "application/json") });
        }
    }
    private sealed class UsuariosFixture : IUsuariosApiClient {
        public Task<string> IniciarSesion(string correo,string contrasena,CancellationToken ct)=>Task.FromResult(SesionRealPrueba.Token(Clave,correo.StartsWith("admin",StringComparison.Ordinal)?"Administrador":"Cajero"));
    }
    private sealed class Host(WebApplication app, Uri direccion) : IAsyncDisposable
    {
        public static async Task<Host> Crear(string ambiente = "Development", string? rolDePrueba = null, Action? alEnviar = null,
            bool adminEnabled = false, bool demoEnabled = true, Action<HttpRequestMessage>? alSolicitar = null)
        {
            var raiz = new DirectoryInfo(AppContext.BaseDirectory);
            while (raiz is not null && !Directory.Exists(Path.Combine(raiz.FullName,"frontend","PuntoMarket.Ventas.Frontend"))) raiz = raiz.Parent;
            var app = FrontendBootstrap.Crear(["--urls","http://127.0.0.1:0","--environment",ambiente,"--contentRoot",Path.Combine(raiz!.FullName,"frontend","PuntoMarket.Ventas.Frontend")], b =>
            {
                b.Configuration.AddInMemoryCollection(new Dictionary<string,string?> { ["DemoLocal:Enabled"]=demoEnabled.ToString(), ["DemoLocal:Key"]=Clave, ["DemoLocal:AdminEnabled"]=adminEnabled.ToString() });
                b.Logging.ClearProviders();
            }, servicios => {
                servicios.AddSingleton(new ValidadorTokenUsuarios(()=>Clave,TimeProvider.System));
                servicios.AddSingleton<IUsuariosApiClient,UsuariosFixture>();
                servicios.AddHttpClient<IVentasApiClient,VentasApiClient>().ConfigurePrimaryHttpMessageHandler(() => new RespuestaHandler(alEnviar: alEnviar, alSolicitar: alSolicitar));
            });
            if (rolDePrueba is not null)
                app.MapGet("/sesion-prueba", async (HttpContext contexto, SesionesUsuario sesiones) => {
                    if(rolDePrueba is not ("Cajero" or "Administrador")) { contexto.Response.StatusCode=401;return; }
                    var id=sesiones.Crear(SesionRealPrueba.Token(Clave,rolDePrueba));
                    await contexto.SignInAsync(AccesoUsuariosPreparado.Esquema,SesionRealPrueba.Principal(sesiones,id),new AuthenticationProperties { ExpiresUtc=sesiones.Identidad(id)!.VenceEn,AllowRefresh=false,IsPersistent=false });
                }).AllowAnonymous();
            await app.StartAsync();
            return new Host(app, new Uri(app.Urls.Single()));
        }
        public HttpClient Cliente() => new(new HttpClientHandler { AllowAutoRedirect=false, CookieContainer=new CookieContainer() }) { BaseAddress=direccion };
        public async ValueTask DisposeAsync() { await app.StopAsync(); await app.DisposeAsync(); }
    }
}
