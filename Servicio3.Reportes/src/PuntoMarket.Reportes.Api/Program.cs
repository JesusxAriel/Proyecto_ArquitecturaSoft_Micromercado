using PuntoMarket.Reportes.Api.Security;
using PuntoMarket.Reportes.Application.DependencyInjection;
using PuntoMarket.Reportes.Application.Ports.Comunes;
using PuntoMarket.Reportes.Infrastructure.DependencyInjection;
using PuntoMarket.Reportes.Infrastructure.Persistence;
using PuntoMarket.Reportes.Infrastructure.Relojes;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AgregarAutenticacionJwt(builder.Configuration);

builder.Services.AddSingleton<IReloj, RelojDelSistema>();
builder.Services.AddSingleton(_ => new ConexionMySql(ObtenerCadenaDeConexion(builder.Configuration)));
builder.Services.AddScoped<IUsuarioActual, UsuarioActualHttp>();
builder.Services.AgregarCasosDeUsoDeVentasPorCategoria().AgregarInfraestructuraDeVentasPorCategoria();
builder.Services.AgregarCasosDeUsoDeListadoVentas().AgregarInfraestructuraDeListadoVentas();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

static string ObtenerCadenaDeConexion(IConfiguration configuracion) =>
    configuracion.GetConnectionString("MySql")
        ?? throw new InvalidOperationException("Falta la cadena de conexión 'ConnectionStrings:MySql'.");

public partial class Program;
