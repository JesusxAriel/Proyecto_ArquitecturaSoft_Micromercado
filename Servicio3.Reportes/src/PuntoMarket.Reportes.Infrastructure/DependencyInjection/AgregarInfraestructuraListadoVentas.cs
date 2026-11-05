using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PuntoMarket.Reportes.Application.Ports.Consultas;
using PuntoMarket.Reportes.Application.Ports.Exportacion;
using PuntoMarket.Reportes.Domain.ListadoVentas;
using PuntoMarket.Reportes.Infrastructure.Exportacion.ListadoVentas;
using PuntoMarket.Reportes.Infrastructure.Exportacion.VentasPorCategoria;
using PuntoMarket.Reportes.Infrastructure.Persistence;

namespace PuntoMarket.Reportes.Infrastructure.DependencyInjection;

public static class AgregarInfraestructuraListadoVentas
{
    public static IServiceCollection AgregarInfraestructuraDeListadoVentas(this IServiceCollection servicios)
    {
        servicios.TryAddSingleton<GeneradorDeGrafico>();
        return servicios
            .AddScoped<IConsultaListadoVentas, MySqlConsultaListadoVentas>()
            .AddSingleton<IExportadorDeReporte<ReporteListadoVentas>, ListadoVentasPdf>()
            .AddSingleton<IExportadorDeReporte<ReporteListadoVentas>, ListadoVentasExcel>()
            .AddSingleton<CreadorDeExportador<ReporteListadoVentas>, CreadorExportadorListadoVentas>();
    }
}
