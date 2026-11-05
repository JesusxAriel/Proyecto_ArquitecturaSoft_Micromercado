using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using PuntoMarket.Ventas.Frontend;
using PuntoMarket.Ventas.Frontend.Clients;
using PuntoMarket.Ventas.Frontend.Security;
namespace PuntoMarket.Ventas.Tests.Frontend;

public sealed class CambiarPasswordTests
{
    private const string Actual = "ActualSoloFixture1!";
    private const string Nueva = "NuevaSoloFixture2@";

    [Theory]
    [InlineData(null, "Ingrese la nueva contraseña.")]
    [InlineData("Ab1!", "La nueva contraseña debe tener al menos 8 caracteres.")]
    [InlineData("abcdefg1!", "La nueva contraseña debe contener al menos una letra mayúscula.")]
    [InlineData("ABCDEFG1!", "La nueva contraseña debe contener al menos una letra minúscula.")]
    [InlineData("Abcdefgh!", "La nueva contraseña debe contener al menos un número.")]
    [InlineData("Abcdefg12", "La nueva contraseña debe contener al menos un carácter especial.")]
    [InlineData("Abcdef1!", null)]
    public void La_politica_valida_cada_requisito_con_mensaje_en_espanol(string? contrasena, string? esperado)
    {
        Assert.Equal(esperado, PoliticaDePassword.PrimerIncumplimiento(contrasena));
    }

    [Fact]
    public async Task La_pagina_voluntaria_se_titula_cambiar_contrasena_y_pide_tres_campos()
    {
        await using var h = await Host.Crear();
        using var c = h.Cliente();
        await h.Login(c);

        var html = WebUtility.HtmlDecode(await c.GetStringAsync("/Account/CambiarPassword"));

        Assert.Contains("<title>Cambiar contraseña - PuntoMarket</title>", html);
        Assert.Contains("Contraseña actual", html);
        Assert.Contains("Nueva contraseña", html);
        Assert.Contains("Confirmar nueva contraseña", html);
        Assert.DoesNotContain("Cambio de contraseña obligatorio", html);
    }

    [Fact]
    public async Task Sin_sesion_la_pagina_redirige_al_login()
    {
        await using var h = await Host.Crear();
        using var c = h.Cliente();

        var respuesta = await c.GetAsync("/Account/CambiarPassword");

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Contains("/Account/Login", respuesta.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Un_cambio_correcto_llama_a_la_api_cierra_la_sesion_y_vuelve_al_login_con_mensaje()
    {
        await using var h = await Host.Crear();
        using var c = h.Cliente();
        await h.Login(c);

        var respuesta = await h.Enviar(c, Actual, Nueva, Nueva);

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal("/Account/Login", respuesta.Headers.Location!.OriginalString);
        var llamada = Assert.Single(h.Api.Llamadas);
        Assert.Equal("/api/autenticacion/cambiar-password", llamada.Ruta);
        Assert.Equal(h.Jwt, llamada.Token);
        using var json = System.Text.Json.JsonDocument.Parse(llamada.Cuerpo!);
        Assert.Equal(new[] { "contrasenaActual", "nuevaContrasena" }, json.RootElement.EnumerateObject().Select(x => x.Name).Order().ToArray());
        var login = WebUtility.HtmlDecode(await c.GetStringAsync("/Account/Login"));
        Assert.Contains("Su contraseña fue actualizada. Inicie sesión con su nueva contraseña.", login);
        Assert.DoesNotContain("Sesión activa:", login);
        var protegida = await c.GetAsync("/Account/CambiarPassword");
        Assert.Equal(HttpStatusCode.Redirect, protegida.StatusCode);
        Assert.DoesNotContain(Actual, h.Logs.Texto);
        Assert.DoesNotContain(Nueva, h.Logs.Texto);
        Assert.DoesNotContain(h.Jwt, h.Logs.Texto);
    }

    [Theory]
    [InlineData(Actual, "corta", "corta", "al menos 8 caracteres")]
    [InlineData(Actual, Nueva, "OtraSoloFixture3#", "no coinciden")]
    [InlineData(Actual, Actual, Actual, "no puede ser igual")]
    [InlineData("", Nueva, Nueva, "Ingrese su contraseña actual.")]
    public async Task Una_entrada_invalida_no_llama_a_la_api_ni_republica_contrasenas(string actual, string nueva, string confirmacion, string mensaje)
    {
        await using var h = await Host.Crear();
        using var c = h.Cliente();
        await h.Login(c);

        var respuesta = await h.Enviar(c, actual, nueva, confirmacion);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var html = WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync());
        Assert.Contains(mensaje, html);
        Assert.Empty(h.Api.Llamadas);
        Assert.DoesNotContain("value=\"" + Nueva + "\"", html);
        Assert.DoesNotContain("value=\"" + Actual + "\"", html);
    }

    [Theory]
    [InlineData(400, "{\"title\":\"La contraseña actual es incorrecta.\"}", "La contraseña actual es incorrecta.")]
    [InlineData(400, "{\"title\":\"Texto interno que no se debe mostrar\"}", "No se pudo cambiar la contraseña. Revise los datos ingresados.")]
    [InlineData(503, "{}", "El servicio de usuarios no está disponible. Intente nuevamente más tarde.")]
    public async Task Los_errores_de_la_api_se_muestran_en_espanol_y_conservan_la_sesion(int codigo, string cuerpo, string mensaje)
    {
        await using var h = await Host.Crear();
        h.Api.Codigo = codigo;
        h.Api.Cuerpo = cuerpo;
        using var c = h.Cliente();
        await h.Login(c);

        var respuesta = await h.Enviar(c, Actual, Nueva, Nueva);

        Assert.Equal((HttpStatusCode)codigo, respuesta.StatusCode);
        Assert.Contains(mensaje, WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync()));
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/Account/CambiarPassword")).StatusCode);
        Assert.DoesNotContain("Texto interno", await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Si_la_api_rechaza_el_token_se_cierra_la_sesion()
    {
        await using var h = await Host.Crear();
        h.Api.Codigo = 401;
        using var c = h.Cliente();
        await h.Login(c);

        var respuesta = await h.Enviar(c, Actual, Nueva, Nueva);

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal("/Account/Login", respuesta.Headers.Location!.OriginalString);
        Assert.Equal(HttpStatusCode.Redirect, (await c.GetAsync("/Account/CambiarPassword")).StatusCode);
    }

    [Fact]
    public async Task El_envio_exige_antiforgery()
    {
        await using var h = await Host.Crear();
        using var c = h.Cliente();
        await h.Login(c);

        var respuesta = await c.PostAsync("/Account/CambiarPassword", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Input.ContrasenaActual", Actual }, { "Input.NuevaContrasena", Nueva }, { "Input.ConfirmarContrasena", Nueva }
        }));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Empty(h.Api.Llamadas);
    }

    [Theory]
    [InlineData("Administrador")]
    [InlineData("Cajero")]
    public async Task Mi_perfil_muestra_usuario_rol_e_inicio_de_sesion_sin_llamar_a_servicios(string rol)
    {
        await using var h = await Host.Crear(rol);
        using var c = h.Cliente();
        await h.Login(c);

        var html = WebUtility.HtmlDecode(await c.GetStringAsync("/Account/Perfil"));

        Assert.Contains("<title>Mi perfil - PuntoMarket</title>", html);
        Assert.Contains(rol == "Administrador" ? "admin" : "cajero1", html);
        Assert.Contains(rol, html);
        Assert.Contains("Nombre de usuario", html);
        Assert.Matches(@"Inicio de sesión</dt>\s*<dd[^>]*>\d{2}/\d{2}/\d{4} \d{2}:\d{2}", html);
        Assert.Contains("href=\"/Account/CambiarPassword\"", html);
        Assert.Empty(h.Api.Llamadas);
    }

    [Theory]
    [InlineData("Administrador", true)]
    [InlineData("Cajero", false)]
    public async Task El_menu_del_usuario_ofrece_perfil_cambio_de_contrasena_y_cierre_de_sesion_en_orden(string rol, bool veUsuarios)
    {
        await using var h = await Host.Crear(rol);
        using var c = h.Cliente();
        await h.Login(c);

        var html = WebUtility.HtmlDecode(await c.GetStringAsync("/Account/Perfil"));

        Assert.Matches("<button[^>]*id=\"userDropdown\"[^>]*aria-expanded=\"false\"|<button[^>]*aria-expanded=\"false\"[^>]*id=\"userDropdown\"", html);
        Assert.Contains("aria-labelledby=\"userDropdown\"", html);
        var perfil = html.IndexOf("Mi perfil</a>", StringComparison.Ordinal);
        var cambio = html.IndexOf("Cambiar contraseña</a>", StringComparison.Ordinal);
        var cierre = html.IndexOf("Cerrar sesión</button>", StringComparison.Ordinal);
        Assert.InRange(perfil, 0, cambio - 1);
        Assert.InRange(cambio, 0, cierre - 1);
        Assert.DoesNotContain(">Salir<", html);
        Assert.Equal(veUsuarios, html.Contains("href=\"/Users\""));
    }

    [Fact]
    public async Task Mi_perfil_sin_sesion_redirige_al_login()
    {
        await using var h = await Host.Crear();
        using var c = h.Cliente();

        var respuesta = await c.GetAsync("/Account/Perfil");

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Contains("/Account/Login", respuesta.Headers.Location!.OriginalString);
    }

    private static string Campo(string html, string campo) =>
        WebUtility.HtmlDecode(Regex.Match(html, "name=\"" + Regex.Escape(campo) + "\"[^>]*value=\"([^\"]*)\"").Groups[1].Value);

    private sealed class LoginFalso(string jwt) : IUsuariosApiClient
    {
        public Task<string> IniciarSesion(string correo, string password, CancellationToken ct) => Task.FromResult(jwt);
    }

    private sealed class ApiRemota
    {
        public int Codigo { get; set; } = 200;
        public string Cuerpo { get; set; } = "{\"cambiada\":true}";
        public List<(string Ruta, string? Token, string? Cuerpo)> Llamadas { get; } = [];
    }

    private sealed class Manejador(ApiRemota api) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage solicitud, CancellationToken ct)
        {
            api.Llamadas.Add((solicitud.RequestUri!.AbsolutePath, solicitud.Headers.Authorization?.Parameter,
                solicitud.Content is null ? null : await solicitud.Content.ReadAsStringAsync(ct)));
            return new HttpResponseMessage((HttpStatusCode)api.Codigo)
            {
                Content = new StringContent(api.Cuerpo, Encoding.UTF8, "application/json")
            };
        }
    }

    private sealed class Registro : ILoggerProvider
    {
        private readonly System.Collections.Concurrent.ConcurrentQueue<string> _lineas = new();
        public string Texto => string.Join("\n", _lineas);
        public ILogger CreateLogger(string nombre) => new Registrador(_lineas);
        public void Dispose() { }

        private sealed class Registrador(System.Collections.Concurrent.ConcurrentQueue<string> lineas) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel level) => true;
            public void Log<TState>(LogLevel level, EventId id, TState state, Exception? e, Func<TState, Exception?, string> f) => lineas.Enqueue(f(state, e));
        }
    }

    private sealed class Host(WebApplication app, string jwt, ApiRemota api, Registro logs) : IAsyncDisposable
    {
        public string Jwt => jwt;
        public ApiRemota Api => api;
        public Registro Logs => logs;

        public static async Task<Host> Crear(string rol = "Cajero")
        {
            var clave = Convert.ToHexString(RandomNumberGenerator.GetBytes(48));
            var jwt = SesionRealPrueba.Token(clave, rol);
            var api = new ApiRemota();
            var logs = new Registro();
            var raiz = new DirectoryInfo(AppContext.BaseDirectory);
            while (raiz is not null && !Directory.Exists(Path.Combine(raiz.FullName, "frontend", "PuntoMarket.Ventas.Frontend"))) raiz = raiz.Parent;
            var app = FrontendBootstrap.Crear(["--urls", "http://127.0.0.1:0", "--environment", "Development", "--contentRoot", Path.Combine(raiz!.FullName, "frontend", "PuntoMarket.Ventas.Frontend")],
                b => { b.Logging.ClearProviders(); b.Logging.AddProvider(logs); },
                s =>
                {
                    s.RemoveAll<IUsuariosApiClient>();
                    s.AddSingleton<IUsuariosApiClient>(new LoginFalso(jwt));
                    s.RemoveAll<ValidadorTokenUsuarios>();
                    s.AddSingleton(new ValidadorTokenUsuarios(() => clave, TimeProvider.System));
                    s.AddHttpClient<IUsuariosCuentaApiClient, UsuariosCuentaApiClient>(c => c.BaseAddress = new Uri("http://usuarios.prueba/"))
                        .ConfigurePrimaryHttpMessageHandler(() => new Manejador(api));
                });
            await app.StartAsync();
            return new Host(app, jwt, api, logs);
        }

        public HttpClient Cliente() => new(new HttpClientHandler { AllowAutoRedirect = false, CookieContainer = new CookieContainer() }) { BaseAddress = new(app.Urls.Single()) };

        public async Task Login(HttpClient c)
        {
            var csrf = Campo(await c.GetStringAsync("/Account/Login"), "__RequestVerificationToken");
            var r = await c.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "__RequestVerificationToken", csrf }, { "Input.Correo", "fixture@example.invalid" }, { "Input.Contrasena", "SoloFixtureNoReal!123" }
            }));
            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        }

        public async Task<HttpResponseMessage> Enviar(HttpClient c, string actual, string nueva, string confirmacion)
        {
            var csrf = Campo(await c.GetStringAsync("/Account/CambiarPassword"), "__RequestVerificationToken");
            return await c.PostAsync("/Account/CambiarPassword", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "__RequestVerificationToken", csrf }, { "Input.ContrasenaActual", actual },
                { "Input.NuevaContrasena", nueva }, { "Input.ConfirmarContrasena", confirmacion }
            }));
        }

        public async ValueTask DisposeAsync() { await app.StopAsync(); await app.DisposeAsync(); }
    }
}
