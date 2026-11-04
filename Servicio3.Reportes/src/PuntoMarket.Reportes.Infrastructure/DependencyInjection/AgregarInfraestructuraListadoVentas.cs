using Microsoft.Extensions.DependencyInjection;
using PuntoMarket.Reportes.Application.Ports.Consultas;
using PuntoMarket.Reportes.Infrastructure.Persistence;

namespace PuntoMarket.Reportes.Infrastructure.DependencyInjection;

public static class AgregarInfraestructuraListadoVentas
{
    public static IServiceCollection AgregarInfraestructuraDeListadoVentas(this IServiceCollection servicios) =>
        servicios.AddScoped<IConsultaListadoVentas, MySqlConsultaListadoVentas>();
}
