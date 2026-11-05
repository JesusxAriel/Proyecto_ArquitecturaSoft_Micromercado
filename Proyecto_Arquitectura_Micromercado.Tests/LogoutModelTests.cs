using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Proyecto_Arquitectura_Micromercado.Pages;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public sealed class LogoutModelTests
{
    [Fact]
    public void Get_no_cierra_sesion_ni_limpia_datos()
    {
        var (model, autenticacion, session) = CrearModelo();
        session.Set("Jwt", [1, 2, 3]);

        var resultado = model.OnGet();

        Assert.Equal("/Index", Assert.IsType<RedirectToPageResult>(resultado).PageName);
        Assert.False(autenticacion.CerroSesion);
        Assert.Equal(["Jwt"], session.Keys);
    }

    [Fact]
    public async Task Post_cierra_cookie_limpia_toda_la_sesion_y_redirige_al_login()
    {
        var (model, autenticacion, session) = CrearModelo();
        session.Set("Jwt", [1, 2, 3]);
        session.Set("NombreParaMostrar", [4, 5]);

        var resultado = await model.OnPostAsync();

        Assert.True(autenticacion.CerroSesion);
        Assert.Equal(CookieAuthenticationDefaults.AuthenticationScheme, autenticacion.Esquema);
        Assert.Empty(session.Keys);
        Assert.Equal("/Login", Assert.IsType<RedirectToPageResult>(resultado).PageName);
    }

    private static (LogoutModel Model, AutenticacionGrabada Autenticacion, SesionPrueba Session)
        CrearModelo()
    {
        var autenticacion = new AutenticacionGrabada();
        var session = new SesionPrueba();
        var contexto = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection()
                .AddSingleton<IAuthenticationService>(autenticacion)
                .BuildServiceProvider()
        };
        contexto.Features.Set<ISessionFeature>(new SessionFeature { Session = session });
        var actionContext = new ActionContext(contexto, new RouteData(), new PageActionDescriptor());
        var model = new LogoutModel
        {
            PageContext = new PageContext(actionContext)
        };
        return (model, autenticacion, session);
    }

    private sealed class AutenticacionGrabada : IAuthenticationService
    {
        public bool CerroSesion { get; private set; }
        public string? Esquema { get; private set; }

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
            AuthenticationProperties? properties) => Task.CompletedTask;

        public Task SignOutAsync(
            HttpContext context,
            string? scheme,
            AuthenticationProperties? properties)
        {
            CerroSesion = true;
            Esquema = scheme;
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
