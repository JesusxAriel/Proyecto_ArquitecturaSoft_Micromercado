using Microsoft.Extensions.DependencyInjection;
using PuntoMarket.Reportes.Application.UseCases.ListadoVentas.GenerarListadoVentas;

namespace PuntoMarket.Reportes.Application.DependencyInjection;

public static class AgregarListadoVentas
{
    public static IServiceCollection AgregarCasosDeUsoDeListadoVentas(this IServiceCollection servicios) =>
        servicios.AddScoped<GenerarListadoVentasCasoDeUso>();
}
