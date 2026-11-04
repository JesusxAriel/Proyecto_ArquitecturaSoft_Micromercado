using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using PuntoMarket.Reportes.Api.Security;
using PuntoMarket.Reportes.Application.DependencyInjection;
using PuntoMarket.Reportes.Application.Ports.Comunes;
using PuntoMarket.Reportes.Infrastructure.DependencyInjection;
using PuntoMarket.Reportes.Infrastructure.Persistence;
using PuntoMarket.Reportes.Infrastructure.Relojes;

var builder = WebApplication.CreateBuilder(args);

var parametrosDeToken = CrearParametrosDeValidacion(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opciones =>
    {
        opciones.MapInboundClaims = false;
        opciones.TokenValidationParameters = parametrosDeToken;
    });
builder.Services.AddAuthorization();

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

static TokenValidationParameters CrearParametrosDeValidacion(IConfiguration configuracion)
{
    var clave = configuracion["Jwt:Key"];
    if (string.IsNullOrWhiteSpace(clave))
        throw new InvalidOperationException("Falta la configuración 'Jwt:Key' (variable de entorno Jwt__Key).");

    return new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = configuracion["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = configuracion["Jwt:Audience"],
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave)),
        ClockSkew = TimeSpan.FromMinutes(1),
        NameClaimType = "unique_name",
        RoleClaimType = "role"
    };
}

static string ObtenerCadenaDeConexion(IConfiguration configuracion) =>
    configuracion.GetConnectionString("MySql")
        ?? throw new InvalidOperationException("Falta la cadena de conexión 'ConnectionStrings:MySql'.");

public partial class Program;
