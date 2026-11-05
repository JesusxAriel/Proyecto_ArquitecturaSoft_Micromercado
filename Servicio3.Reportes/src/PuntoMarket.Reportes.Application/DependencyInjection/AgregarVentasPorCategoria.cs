using Microsoft.Extensions.DependencyInjection;
using PuntoMarket.Reportes.Application.UseCases.VentasPorCategoria.GenerarVentasPorCategoria;
using PuntoMarket.Reportes.Application.UseCases.VentasPorCategoria.ListarCategorias;

namespace PuntoMarket.Reportes.Application.DependencyInjection;

public static class AgregarVentasPorCategoria
{
    public static IServiceCollection AgregarCasosDeUsoDeVentasPorCategoria(this IServiceCollection servicios) =>
        servicios
            .AddScoped<GenerarVentasPorCategoriaCasoDeUso>()
            .AddScoped<ListarCategoriasCasoDeUso>();
}
