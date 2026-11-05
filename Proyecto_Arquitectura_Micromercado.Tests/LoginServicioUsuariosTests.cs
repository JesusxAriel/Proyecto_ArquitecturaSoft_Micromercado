using System.Net;
using System.Security.Claims;
using System.Text;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Proyecto_Arquitectura_Micromercado.Application.Security;
using Proyecto_Arquitectura_Micromercado.Dtos.Usuarios;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Web;
using Proyecto_Arquitectura_Micromercado.Pages;
using Proyecto_Arquitectura_Micromercado.Pages.Shared;
using Proyecto_Arquitectura_Micromercado.Servicios;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public sealed class LoginServicioUsuariosTests
{
    private static readonly DateTimeOffset ExpiracionJwt = DateTimeOffset.UtcNow.AddMinutes(50);

    [Fact]
    public async Task Login_valida_token_crea_cookie_y_guarda_datos_en_sesion_de_servidor()
    {
        var api = CrearCliente(HttpStatusCode.OK, """{"token":"jwt-de-prueba"}""");
        var autenticacion = new AutenticacionGrabada();
        var session = new SesionPrueba();
        session.Set("dato-anterior", Encoding.UTF8.GetBytes("anterior"));
        var model = CrearModelo(api, new ValidadorJwtPrueba(CrearJwtValidado()), autenticacion, session);

        var resultado = await model.OnPostAsync(default);

        Assert.IsType<LocalRedirectResult>(resultado);
        Assert.Equal("/Ventas", Assert.IsType<LocalRedirectResult>(resultado).Url);
        Assert.Equal("jwt-de-prueba", session.GetString(VariablesDeSesion.Jwt));
        Assert.Equal("Ana Pérez", session.GetString(VariablesDeSesion.NombreParaMostrar));
        Assert.Null(session.GetString("dato-anterior"));
        Assert.True(autenticacion.CerroSesionAnterior);
        var principal = Assert.IsType<ClaimsPrincipal>(autenticacion.Principal);
        Assert.Equal("42", principal.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal("ana", principal.FindFirstValue(ClaimTypes.Name));
        Assert.Equal("Administrador", principal.FindFirstValue(ClaimTypes.Role));
        Assert.Equal("False", principal.FindFirstValue(ClaimsDeIdentidad.DebeCambiarPassword));
        Assert.Equal(
            ExpiracionJwt.ToUnixTimeSeconds().ToString(),
            principal.FindFirstValue(ClaimsDeIdentidad.ExpiracionJwt));
    }

    [Fact]
    public async Task Login_rechaza_token_invalido_sin_crear_cookie_ni_sesion()
    {
        var api = CrearCliente(HttpStatusCode.OK, """{"token":"jwt-invalido"}""");
        var autenticacion = new AutenticacionGrabada();
        var session = new SesionPrueba();
        var model = CrearModelo(
            api,
            new ValidadorJwtPrueba(exception: new SecurityTokenException("token inválido")),
            autenticacion,
            session);

        var resultado = await model.OnPostAsync(default);

        Assert.IsType<PageResult>(resultado);
        Assert.Contains("No se pudo validar", model.MensajeDeError);
        Assert.Null(autenticacion.Principal);
        Assert.Null(session.GetString(VariablesDeSesion.Jwt));
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "Correo o contraseña incorrectos.")]
    [InlineData(HttpStatusCode.BadRequest, "Ingrese un correo válido y una contraseña.")]
    [InlineData(HttpStatusCode.InternalServerError, "El servicio de usuarios no está disponible. Intente nuevamente.")]
    public async Task Login_muestra_mensaje_especifico_segun_respuesta_http(
        HttpStatusCode status,
        string mensaje)
    {
        var api = CrearCliente(status, """{"title":"error"}""");
        var autenticacion = new AutenticacionGrabada();
        var model = CrearModelo(api, new ValidadorJwtPrueba(CrearJwtValidado()), autenticacion);
        model.Input.Contrasena = "Secreto123!";

        var resultado = await model.OnPostAsync(default);

        Assert.IsType<PageResult>(resultado);
        Assert.Equal(mensaje, model.MensajeDeError);
        Assert.Equal(string.Empty, model.Input.Contrasena);
        Assert.Null(autenticacion.Principal);
    }

    [Fact]
    public async Task Login_muestra_indisponibilidad_si_falla_la_conexion()
    {
        var api = CrearCliente(
            HttpStatusCode.OK,
            """{"token":"irrelevante"}""",
            new HttpRequestException("fallo"));
        var model = CrearModelo(api, new ValidadorJwtPrueba(CrearJwtValidado()), new AutenticacionGrabada());

        var resultado = await model.OnPostAsync(default);

        Assert.IsType<PageResult>(resultado);
        Assert.Contains("no está disponible", model.MensajeDeError);
    }

    [Fact]
    public async Task Login_redirige_a_cambio_de_contrasena_si_el_claim_lo_exige()
    {
        var api = CrearCliente(HttpStatusCode.OK, """{"token":"jwt-de-prueba"}""");
        var jwt = CrearJwtValidado(debeCambiarPassword: true);
        var model = CrearModelo(api, new ValidadorJwtPrueba(jwt), new AutenticacionGrabada());

        var resultado = await model.OnPostAsync(default);

        Assert.Equal("/Users/CambiarPassword", Assert.IsType<RedirectToPageResult>(resultado).PageName);
    }

    [Fact]
    public async Task Login_no_acepta_return_url_remota()
    {
        var api = CrearCliente(HttpStatusCode.OK, """{"token":"jwt-de-prueba"}""");
        var model = CrearModelo(api, new ValidadorJwtPrueba(CrearJwtValidado()), new AutenticacionGrabada());
        model.ReturnUrl = "https://sitio-malicioso.example/";

        var resultado = await model.OnPostAsync(default);

        Assert.Equal("/Index", Assert.IsType<RedirectToPageResult>(resultado).PageName);
    }

    [Theory]
    [InlineData(30, 50, 30)]
    [InlineData(30, 10, 10)]
    [InlineData(30, 20, 15, 15)]
    public void Expiracion_de_cookie_no_supera_los_limites_de_cookie_ni_del_jwt(
        int duracionMaximaMinutos,
        int expiracionJwtMinutos,
        int expiracionEsperadaMinutos,
        int? expiracionExistenteMinutos = null)
    {
        var ahora = DateTimeOffset.UtcNow;
        var expiracion = ExpiracionCookieJwt.CalcularExpiracion(
            ahora,
            TimeSpan.FromMinutes(duracionMaximaMinutos),
            ahora.AddMinutes(expiracionJwtMinutos),
            expiracionExistenteMinutos is int minutos
                ? ahora.AddMinutes(minutos)
                : null);

        Assert.Equal(ahora.AddMinutes(expiracionEsperadaMinutos), expiracion);
    }

    private static JwtValidado CrearJwtValidado(bool debeCambiarPassword = false)
    {
        var identidad = new ClaimsIdentity(
            [
                new Claim("sub", "42"),
                new Claim("unique_name", "ana"),
                new Claim("nombre", "Ana Pérez"),
                new Claim("role", "Administrador"),
                new Claim("debeCambiarPassword", debeCambiarPassword.ToString().ToLowerInvariant())
            ],
            "JWT validado");
        return new JwtValidado(new ClaimsPrincipal(identidad), ExpiracionJwt);
    }

    private static LoginModel CrearModelo(
        UsuariosApiClient cliente,
        IValidadorJwt validador,
        AutenticacionGrabada autenticacion,
        SesionPrueba? session = null)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = new ServiceCollection()
            .AddSingleton<IAuthenticationService>(autenticacion)
            .BuildServiceProvider();
        httpContext.Features.Set<ISessionFeature>(new SessionFeature { Session = session ?? new SesionPrueba() });
        var actionContext = new ActionContext(httpContext, new RouteData(), new PageActionDescriptor());
        return new LoginModel(cliente, validador)
        {
            PageContext = new PageContext(actionContext),
            Url = new UrlHelper(actionContext),
            Input = new LoginModel.CredencialesInput
            {
                Correo = "ana@ejemplo.com",
                Contrasena = "Secreto123!"
            },
            ReturnUrl = "/Ventas"
        };
    }

    private static UsuariosApiClient CrearCliente(
        HttpStatusCode status,
        string contenido,
        HttpRequestException? fallo = null)
    {
        var handler = new LoginHttpHandler(status, contenido, fallo);
        return new UsuariosApiClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://usuarios.test/")
        });
    }

    private sealed class LoginHttpHandler(
        HttpStatusCode status,
        string contenido,
        HttpRequestException? fallo) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (fallo is not null)
                throw fallo;

            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(contenido, Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class ValidadorJwtPrueba(
        JwtValidado? resultado = null,
        Exception? exception = null) : IValidadorJwt
    {
        public ClaimsPrincipal Validar(string token) => ValidarConExpiracion(token).Principal;

        public JwtValidado ValidarConExpiracion(string token)
        {
            if (exception is not null)
                throw exception;
            return resultado!;
        }
    }

    private sealed class AutenticacionGrabada : IAuthenticationService
    {
        public ClaimsPrincipal? Principal { get; private set; }
        public bool CerroSesionAnterior { get; private set; }

        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme) =>
            Task.FromResult(AuthenticateResult.NoResult());

        public Task ChallengeAsync(
            HttpContext context,
            string? scheme,
            AuthenticationProperties? properties) => Task.CompletedTask;

        public Task ForbidAsync(
            HttpContext context,
            string? scheme,
            AuthenticationProperties? properties) => Task.CompletedTask;

        public Task SignInAsync(
            HttpContext context,
            string? scheme,
            ClaimsPrincipal principal,
            AuthenticationProperties? properties)
        {
            Principal = principal;
            return Task.CompletedTask;
        }

        public Task SignOutAsync(
            HttpContext context,
            string? scheme,
            AuthenticationProperties? properties)
        {
            CerroSesionAnterior = true;
            Principal = null;
            return Task.CompletedTask;
        }
    }

    private sealed class SesionPrueba : ISession
    {
        private readonly Dictionary<string, byte[]> valores = new(StringComparer.Ordinal);

        public bool IsAvailable => true;
        public string Id => "sesion-prueba";
        public IEnumerable<string> Keys => valores.Keys;
        public void Clear() => valores.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => valores.Remove(key);
        public void Set(string key, byte[] value) => valores[key] = value;
        public bool TryGetValue(string key, [NotNullWhen(true)] out byte[]? value) =>
            valores.TryGetValue(key, out value);
    }

    private sealed class SessionFeature : ISessionFeature
    {
        public ISession Session { get; set; } = null!;
    }
}
