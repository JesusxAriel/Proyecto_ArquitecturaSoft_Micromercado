using Microsoft.Extensions.DependencyInjection;
using PuntoMarket.Reportes.Application.UseCases.VentasPorCategoria.GenerarVentasPorCategoria;

namespace PuntoMarket.Reportes.Application.DependencyInjection;

public static class AgregarVentasPorCategoria
{
    public static IServiceCollection AgregarCasosDeUsoDeVentasPorCategoria(this IServiceCollection servicios) =>
        servicios.AddScoped<GenerarVentasPorCategoriaCasoDeUso>();
}
