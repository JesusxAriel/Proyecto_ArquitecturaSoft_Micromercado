using Microsoft.Extensions.DependencyInjection.Extensions;
using PuntoMarket.Ventas.Frontend.Clients;
using PuntoMarket.Ventas.Frontend.Security;
namespace PuntoMarket.Ventas.Frontend.Configuration;

public static class PreparacionUsuarios
{
    public static IServiceCollection AgregarPreparacionUsuarios(this IServiceCollection servicios, IConfiguration? configuracion = null)
    {
        var direccion = configuracion?["UsuariosApi:BaseUrl"];
        if (string.IsNullOrWhiteSpace(direccion) && Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true")
            throw new InvalidOperationException("Configure UsuariosApi:BaseUrl con la dirección interna de Usuarios para Docker.");
        if (!Uri.TryCreate(direccion ?? "http://127.0.0.1:5003/", UriKind.Absolute, out var usuariosUri) ||
            usuariosUri.Scheme is not ("http" or "https") || usuariosUri.AbsolutePath != "/" ||
            usuariosUri.UserInfo.Length != 0 || usuariosUri.Query.Length != 0 || usuariosUri.Fragment.Length != 0)
            throw new InvalidOperationException("UsuariosApi:BaseUrl debe ser HTTP/HTTPS, sin credenciales, ruta ni parámetros.");
        servicios.AddMemoryCache();
        servicios.AddSingleton<ActivacionesPendientes>();
        servicios.TryAddSingleton<TimeProvider>(TimeProvider.System);
        servicios.AddSingleton(sp => new ValidadorTokenUsuarios(
            // Lectura diferida de la clave externa: solo valida JWT emitidos por Usuarios.
            () => File.ReadAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "puntomarket-demo-secrets", "jwt-demo-key.txt")),
            sp.GetRequiredService<TimeProvider>()));
        servicios.AddSingleton<SesionesUsuario>();
        servicios.AddSingleton<ControlAltaUsuarios>();
        servicios.AddScoped<ResolucionSesion>();
        servicios.AddHttpClient<IUsuariosApiClient, UsuariosApiClient>(c => {
            c.BaseAddress = usuariosUri; c.Timeout = TimeSpan.FromSeconds(10);
            c.MaxResponseContentBufferSize = 65536;
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false, UseProxy = false }).RemoveAllLoggers();
        servicios.AddHttpClient<IUsuariosGestionApiClient, UsuariosGestionApiClient>(c => {
            c.BaseAddress = usuariosUri; c.Timeout = TimeSpan.FromSeconds(15);
            c.MaxResponseContentBufferSize = 4 * 1024 * 1024;
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false, UseProxy = false }).RemoveAllLoggers();
        servicios.AddHttpClient<IUsuariosCuentaApiClient, UsuariosCuentaApiClient>(c => {
            c.BaseAddress = usuariosUri; c.Timeout = TimeSpan.FromSeconds(15); c.MaxResponseContentBufferSize = 65536;
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false, UseProxy = false }).RemoveAllLoggers();
        servicios.AddHttpClient<IActivacionApiClient, ActivacionApiClient>(c => {
            c.BaseAddress = usuariosUri; c.Timeout = TimeSpan.FromSeconds(15); c.MaxResponseContentBufferSize = 65536;
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false, UseProxy = false }).RemoveAllLoggers();
        servicios.AgregarAccesoUsuariosPreparado();
        return servicios;
    }
}
