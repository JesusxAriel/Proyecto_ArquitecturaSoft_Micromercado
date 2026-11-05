using PuntoMarket.Reportes.Api.Documentation;
using PuntoMarket.Reportes.Api.Security;
using PuntoMarket.Reportes.Application.DependencyInjection;
using PuntoMarket.Reportes.Application.Ports.Comunes;
using PuntoMarket.Reportes.Infrastructure.DependencyInjection;
using PuntoMarket.Reportes.Infrastructure.Persistence;
using PuntoMarket.Reportes.Infrastructure.Relojes;


namespace PuntoMarket.Reportes.Api;
public static class ReportesBootstrap
{
public static WebApplication Crear(string[] args, Action<WebApplicationBuilder>? configurar = null)
{
var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args, ApplicationName = typeof(ReportesBootstrap).Assembly.FullName });
configurar?.Invoke(builder);

var documentacionLocal = builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("LocalDemo");
if (documentacionLocal) builder.Services.AgregarDocumentacionReportes();
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AgregarAutenticacionJwt(builder.Configuration);

builder.Services.AddSingleton<IReloj, RelojDelSistema>();
builder.Services.AddSingleton(_ => new ConexionMySql(ObtenerCadenaDeConexion(builder.Configuration)));
builder.Services.AddScoped<IUsuarioActual, UsuarioActualHttp>();
builder.Services.AgregarCasosDeUsoDeVentasPorCategoria().AgregarInfraestructuraDeVentasPorCategoria();
builder.Services.AgregarCasosDeUsoDeListadoVentas().AgregarInfraestructuraDeListadoVentas();

var app = builder.Build();

if (documentacionLocal)
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("v1/swagger.json", "PuntoMarket.Reportes v1");
        options.ConfigObject.PersistAuthorization = false;
    });
}
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

return app;

}
static string ObtenerCadenaDeConexion(IConfiguration configuracion) =>
    configuracion.GetConnectionString("MySql")
        ?? throw new InvalidOperationException("Falta la cadena de conexión 'ConnectionStrings:MySql'.");

}
