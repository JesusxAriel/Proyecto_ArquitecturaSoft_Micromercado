using PuntoMarket.Usuarios.Application.DependencyInjection;
using PuntoMarket.Usuarios.Api.DependencyInjection;
using PuntoMarket.Usuarios.Api.Documentation;
using PuntoMarket.Usuarios.Infrastructure.DependencyInjection;
namespace PuntoMarket.Usuarios.Api;
public static class UsuariosBootstrap
{
    public static WebApplication Crear(string[] args, Action<WebApplicationBuilder>? configurar = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args, ApplicationName = typeof(UsuariosBootstrap).Assembly.FullName });
        configurar?.Invoke(builder);
        var localDemo = builder.Environment.IsEnvironment("LocalDemo");
        if (localDemo) builder.WebHost.UseUrls("http://127.0.0.1:5003");
        var documentar = builder.Environment.IsDevelopment() || localDemo;
        builder.Services.AddControllers();
        builder.Services.AgregarGestionDeUsuarios();
        builder.Services.AgregarAutenticacionDeUsuarios();
        builder.Services.AgregarInfraestructuraGestion(builder.Configuration);
        builder.Services.AgregarAutenticacionJwt(builder.Configuration);
        builder.Services.AddAuthorization();
        if (documentar) builder.Services.AgregarDocumentacionUsuarios();
        var app = builder.Build();
        // LocalDemo solo escucha loopback HTTP; otros entornos conservan HTTPS.
        if (!localDemo) app.UseHttpsRedirection();
        if (documentar)
        {
            app.UseSwagger();
            app.UseSwaggerUI(o => { o.SwaggerEndpoint("v1/swagger.json", "PuntoMarket.Usuarios v1"); o.ConfigObject.PersistAuthorization = false; });
        }
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        return app;
    }
}
