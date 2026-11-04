using PuntoMarket.Usuarios.Application.DependencyInjection;
using PuntoMarket.Usuarios.Api.DependencyInjection;
using PuntoMarket.Usuarios.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AgregarGestionDeUsuarios();
builder.Services.AgregarInfraestructuraGestion(builder.Configuration);
builder.Services.AgregarAutenticacionJwt(builder.Configuration);
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
