using PuntoMarket.Reportes.Domain.ListadoVentas;
using PuntoMarket.Reportes.Infrastructure.Exportacion.Comunes;
using PuntoMarket.Reportes.Infrastructure.Exportacion.VentasPorCategoria;

namespace PuntoMarket.Reportes.Infrastructure.Exportacion.ListadoVentas;

public sealed record GraficosDeListadoVentas(ImagenDeGrafico MontoPorDia, ImagenDeGrafico ParticipacionPorCliente)
{
    public const int MaximoDeClientes = 5;

    public static GraficosDeListadoVentas? Crear(GeneradorDeGrafico generador, ReporteListadoVentas reporte)
    {
        var montoPorDia = reporte.MontoPorDia();
        if (montoPorDia.Count == 0)
            return null;

        return new GraficosDeListadoVentas(
            generador.MontoPorDia(montoPorDia),
            generador.ParticipacionPorCliente(reporte.ParticipacionPorCliente(MaximoDeClientes)));
    }
}
