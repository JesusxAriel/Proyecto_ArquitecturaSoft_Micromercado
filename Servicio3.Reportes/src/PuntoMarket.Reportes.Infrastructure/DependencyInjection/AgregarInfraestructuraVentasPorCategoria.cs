using Microsoft.Extensions.DependencyInjection;
using PuntoMarket.Reportes.Application.Ports.Consultas;
using PuntoMarket.Reportes.Application.Ports.Exportacion;
using PuntoMarket.Reportes.Domain.VentasPorCategoria;
using PuntoMarket.Reportes.Infrastructure.Exportacion.VentasPorCategoria;
using PuntoMarket.Reportes.Infrastructure.Persistence;

namespace PuntoMarket.Reportes.Infrastructure.DependencyInjection;

public static class AgregarInfraestructuraVentasPorCategoria
{
    public static IServiceCollection AgregarInfraestructuraDeVentasPorCategoria(this IServiceCollection servicios) =>
        servicios
            .AddScoped<IConsultaVentasPorCategoria, MySqlConsultaVentasPorCategoria>()
            .AddScoped<IConsultaTendenciaDeVentas, MySqlConsultaTendenciaDeVentas>()
            .AddScoped<IConsultaProductosMasVendidos, MySqlConsultaProductosMasVendidos>()
            .AddSingleton<GeneradorDeGrafico>()
            .AddSingleton<IExportadorDeReporte<ReporteVentasPorCategoria>, VentasPorCategoriaPdf>()
            .AddSingleton<IExportadorDeReporte<ReporteVentasPorCategoria>, VentasPorCategoriaExcel>()
            .AddSingleton<CreadorDeExportador<ReporteVentasPorCategoria>, CreadorExportadorVentasPorCategoria>();
}
