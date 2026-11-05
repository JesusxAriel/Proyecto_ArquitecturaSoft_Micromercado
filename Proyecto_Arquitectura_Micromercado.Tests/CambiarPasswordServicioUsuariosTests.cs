using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Proyecto_Arquitectura_Micromercado.Application.Security;
using Proyecto_Arquitectura_Micromercado.Dtos.Usuarios;
using Proyecto_Arquitectura_Micromercado.Pages.Shared;
using Proyecto_Arquitectura_Micromercado.Pages.Users;
using Proyecto_Arquitectura_Micromercado.Servicios;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public sealed class CambiarPasswordServicioUsuariosTests
{
    [Fact]
    public async Task Cliente_envia_solo_contrasena_actual_y_nueva_y_devuelve_confirmacion()
    {
        var handler = new HttpHandler(HttpStatusCode.OK, """{"cambiada":true}""");
        using var clienteHttp = CrearHttpClient(handler);
        var cliente = new UsuariosApiClient(clienteHttp);

        var respuesta = await cliente.CambiarPasswordAsync(
            new CambiarPasswordDto("Actual1!", "NuevaSegura2!"));

        Assert.True(respuesta.Cambiada);
        Assert.Equal(HttpMethod.Post, handler.Metodo);
        Assert.Equal("/api/autenticacion/cambiar-password", handler.Ruta);
        using var json = JsonDocument.Parse(handler.Cuerpo!);
        Assert.Equal(2, json.RootElement.EnumerateObject().Count());
        Assert.Equal("Actual1!", json.RootElement.GetProperty("contrasenaActual").GetString());
        Assert.Equal("NuevaSegura2!", json.RootElement.GetProperty("nuevaContrasena").GetString());
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, """{"title":"La contraseña actual es incorrecta."}""",
        "La contraseña actual es incorrecta.")]
    [InlineData(HttpStatusCode.BadRequest, """{"title":"La nueva contraseña no cumple con los requisitos de seguridad."}""",
        "La nueva contraseña no cumple con los requisitos de seguridad.")]
    [InlineData(HttpStatusCode.Forbidden, """{"title":"La cuenta no está activa."}""",
        "La cuenta ya no está activa. Contacte al administrador.")]
    [InlineData(HttpStatusCode.Unauthorized, """{"title":"JWT inválido"}""",
        "La sesión con Servicio 2 venció. Inicie sesión nuevamente.")]
    public async Task Cliente_traduce_errores_http_sin_exponer_el_detalle_remoto(
        HttpStatusCode status,
        string contenido,
        string mensajeEsperado)
    {
        var handler = new HttpHandler(status, contenido);
        using var clienteHttp = CrearHttpClient(handler);
        var cliente = new UsuariosApiClient(clienteHttp);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => cliente.CambiarPasswordAsync(new CambiarPasswordDto("a", "b")));

        Assert.Equal(status, exception.StatusCode);
        Assert.Equal(mensajeEsperado, exception.Message);
        Assert.DoesNotContain("JWT inválido", exception.Message);
    }

    [Fact]
    public async Task Formulario_valida_confirmacion_localmente_y_no_llama_a_la_api()
    {
        var handler = new HttpHandler(HttpStatusCode.OK, """{"cambiada":true}""");
        using var clienteHttp = CrearHttpClient(handler);
        var model = CrearModelo(new UsuariosApiClient(clienteHttp), new AutenticacionGrabada());
        model.Input.ConfirmarContrasena = "Diferente1!";

        var resultado = await model.OnPostAsync(default);

        Assert.IsType<PageResult>(resultado);
        Assert.Equal("La nueva contraseña y la confirmación no coinciden.", model.MensajeDeError);
        Assert.Equal(0, handler.CantidadSolicitudes);
    }

    [Fact]
    public async Task Formulario_muestra_mensaje_temporal_tras_cambio_exitoso_y_redirige_al_inicio()
    {
        var handler = new HttpHandler(HttpStatusCode.OK, """{"cambiada":true}""");
        using var clienteHttp = CrearHttpClient(handler);
        var model = CrearModelo(new UsuariosApiClient(clienteHttp), new AutenticacionGrabada());

        var resultado = await model.OnPostAsync(default);

        Assert.Equal("/Index", Assert.IsType<RedirectToPageResult>(resultado).PageName);
        Assert.Equal(
            "Tu nueva contraseña se usará en tu próximo inicio de sesión.",
            model.TempData["MensajeCambioPassword"]);
    }

    [Fact]
    public async Task Formulario_no_muestra_mensaje_temporal_si_el_cambio_falla()
    {
        var handler = new HttpHandler(HttpStatusCode.BadRequest, """{"title":"La contraseña actual es incorrecta."}""");
        using var clienteHttp = CrearHttpClient(handler);
        var model = CrearModelo(new UsuariosApiClient(clienteHttp), new AutenticacionGrabada());

        var resultado = await model.OnPostAsync(default);

        Assert.IsType<PageResult>(resultado);
        Assert.Null(model.TempData["MensajeCambioPassword"]);
    }

    [Fact]
    public async Task Formulario_actualiza_claim_preservando_demas_claims_y_expiracion_jwt()
    {
        var handler = new HttpHandler(HttpStatusCode.OK, """{"cambiada":true}""");
        using var clienteHttp = CrearHttpClient(handler);
        var autenticacion = new AutenticacionGrabada();
        var model = CrearModelo(new UsuariosApiClient(clienteHttp), autenticacion);

        var resultado = await model.OnPostAsync(default);

        Assert.Equal("/Index", Assert.IsType<RedirectToPageResult>(resultado).PageName);
        var principal = Assert.IsType<ClaimsPrincipal>(autenticacion.Principal);
        Assert.Equal("42", principal.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal("ana", principal.FindFirstValue(ClaimTypes.Name));
        Assert.Equal("Administrador", principal.FindFirstValue(ClaimTypes.Role));
        Assert.Equal("preservar", principal.FindFirstValue("otro-claim"));
        Assert.Equal("1799999999", principal.FindFirstValue(ClaimsDeIdentidad.ExpiracionJwt));
        Assert.Equal(bool.FalseString, principal.FindFirstValue(ClaimsDeIdentidad.DebeCambiarPassword));
    }

    [Fact]
    public async Task Formulario_cierra_sesion_si_el_jwt_es_rechazado_por_el_servicio()
    {
        var handler = new HttpHandler(HttpStatusCode.Unauthorized, """{"title":"No autorizado"}""");
        using var clienteHttp = CrearHttpClient(handler);
        var autenticacion = new AutenticacionGrabada();
        var session = new SesionPrueba();
        session.Set(VariablesDeSesion.Jwt, Encoding.UTF8.GetBytes("token-de-sesion"));
        var model = CrearModelo(new UsuariosApiClient(clienteHttp), autenticacion, session);

        var resultado = await model.OnPostAsync(default);

        var redirect = Assert.IsType<RedirectToPageResult>(resultado);
        Assert.Equal("/Login", redirect.PageName);
        Assert.Equal(true, redirect.RouteValues!["SesionVencida"]);
        Assert.True(autenticacion.CerroSesionAnterior);
        Assert.Empty(session.Keys);
    }

    private static HttpClient CrearHttpClient(HttpHandler handler) =>
        new(handler) { BaseAddress = new Uri("https://usuarios.test/") };

    private static CambiarPasswordModel CrearModelo(
        UsuariosApiClient cliente,
        AutenticacionGrabada autenticacion,
        SesionPrueba? session = null)
    {
        var identidad = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "42"),
            new Claim(ClaimTypes.Name, "ana"),
            new Claim(ClaimTypes.Role, "Administrador"),
            new Claim(ClaimsDeIdentidad.DebeCambiarPassword, bool.TrueString),
            new Claim(ClaimsDeIdentidad.ExpiracionJwt, "1799999999"),
            new Claim("otro-claim", "preservar")
        ],
        CookieAuthenticationDefaults.AuthenticationScheme);
        var contexto = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identidad),
            RequestServices = new ServiceCollection()
                .AddSingleton<IAuthenticationService>(autenticacion)
                .BuildServiceProvider()
        };
        contexto.Features.Set<ISessionFeature>(
            new SessionFeature { Session = session ?? new SesionPrueba() });
        var actionContext = new ActionContext(contexto, new RouteData(), new PageActionDescriptor());
        var model = new CambiarPasswordModel(cliente)
        {
            PageContext = new PageContext(actionContext),
            Url = new UrlHelper(actionContext),
            Input = new CambiarPasswordModel.CambiarPasswordInput
            {
                ContrasenaActual = "Actual1!",
                NuevaContrasena = "NuevaSegura2!",
                ConfirmarContrasena = "NuevaSegura2!"
            }
        };
        model.TempData = new TempDataDictionary(contexto, new TempDataProviderStub());
        return model;
    }

    private sealed class TempDataProviderStub : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }

    private sealed class HttpHandler(HttpStatusCode status, string contenido) : HttpMessageHandler
    {
        public HttpMethod? Metodo { get; private set; }
        public string? Ruta { get; private set; }
        public string? Cuerpo { get; private set; }
        public int CantidadSolicitudes { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CantidadSolicitudes++;
            Metodo = request.Method;
            Ruta = request.RequestUri?.AbsolutePath;
            Cuerpo = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status)
            {
                Content = new StringContent(contenido, Encoding.UTF8, "application/json")
            };
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
