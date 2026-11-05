using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using PuntoMarket.Ventas.Frontend.Clients;
using PuntoMarket.Ventas.Frontend.Configuration;
using PuntoMarket.Ventas.Frontend.Security;

namespace PuntoMarket.Ventas.Frontend;

public static class FrontendBootstrap
{
    public static WebApplication Crear(string[] args, Action<WebApplicationBuilder>? configurar = null, Action<IServiceCollection>? configurarServicios = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args, ApplicationName = typeof(FrontendBootstrap).Assembly.FullName });
        if (string.IsNullOrWhiteSpace(builder.Configuration["urls"])) builder.WebHost.UseUrls("http://localhost:5100");
        configurar?.Invoke(builder);
        // Hosting.Diagnostics incluye la consulta completa del enlace de activación en RequestStarting.
        // No habilitar registro de cuerpos/consultas en proxies ni middleware de HTTP logging.
        builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Warning);
        builder.Services.AddRazorPages();
        builder.Services.AddDataProtection();
        builder.Services.Configure<KeyManagementOptions>(o => o.XmlRepository = new ClavesTemporales());
        builder.Services.AddMemoryCache();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddAuthorization(o => o.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        var uri = new Uri(builder.Configuration["VentasApi:BaseUrl"] ?? "http://localhost:5002/");
        if (!uri.IsLoopback || uri.Scheme != "http" || uri.Port != 5002 || uri.AbsolutePath != "/") throw new InvalidOperationException("Esta fase requiere API local en 5002.");
        builder.Services.AddHttpClient<IVentasApiClient, VentasApiClient>(c => { c.BaseAddress = uri; c.Timeout = TimeSpan.FromSeconds(10); })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false }).RemoveAllLoggers();
        var reportesUri = new Uri(builder.Configuration["ReportesApi:BaseUrl"] ?? "http://localhost:5012/");
        if (reportesUri.AbsolutePath != "/" || !string.IsNullOrEmpty(reportesUri.Query) || !string.IsNullOrEmpty(reportesUri.UserInfo) || !string.IsNullOrEmpty(reportesUri.Fragment) ||
            (reportesUri.Scheme != "https" && !(reportesUri.IsLoopback && reportesUri.Scheme == "http")))
            throw new InvalidOperationException("ReportesApi:BaseUrl requiere HTTPS o HTTP local, sin credenciales, ruta ni consulta.");
        builder.Services.AddHttpClient<IReportesApiClient, ReportesApiClient>(c => { c.BaseAddress = reportesUri; c.Timeout = TimeSpan.FromSeconds(60); c.MaxResponseContentBufferSize = 32 * 1024 * 1024; })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false }).RemoveAllLoggers();
        builder.Services.AgregarPreparacionUsuarios(builder.Configuration); // Único mecanismo: JWT de Usuarios y sesión protegida en el servidor.
        configurarServicios?.Invoke(builder.Services);
        var app = builder.Build();
        app.UseExceptionHandler("/Error");
        app.Use(async (context, next) =>
        {
            // Las cookies Demo antiguas no se autentican ni se convierten en sesiones reales.
            if (context.Request.Cookies.ContainsKey("PuntoMarket.Demo.Session"))
                context.Response.Cookies.Delete("PuntoMarket.Demo.Session", new CookieOptions { Path = "/", HttpOnly = true, SameSite = SameSiteMode.Strict });
            context.Response.Headers.CacheControl = "no-store";
            context.Response.Headers["Referrer-Policy"] = "no-referrer";
            await next();
        });
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthentication();
        app.Use(async (context, next) => {
            if (context.Request.Cookies.ContainsKey(AccesoUsuariosPreparado.Cookie) && context.User.Identity?.IsAuthenticated != true)
                await Microsoft.AspNetCore.Authentication.AuthenticationHttpContextExtensions.SignOutAsync(context, AccesoUsuariosPreparado.Esquema);
            await next();
        });
        app.UseAuthorization();
        app.MapRazorPages();
        return app;
    }
}
