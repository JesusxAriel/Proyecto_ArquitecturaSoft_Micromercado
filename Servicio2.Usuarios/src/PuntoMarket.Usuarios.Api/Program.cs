using PuntoMarket.Usuarios.Application.DependencyInjection;
using PuntoMarket.Usuarios.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AgregarGestionDeUsuarios();
builder.Services.AgregarInfraestructuraGestion(builder.Configuration);

var app = builder.Build();

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
