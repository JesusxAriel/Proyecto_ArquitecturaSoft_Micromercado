using PuntoMarket.Reportes.Domain.VentasPorCategoria;
using PuntoMarket.Reportes.Infrastructure.Exportacion.Comunes;

namespace PuntoMarket.Reportes.Infrastructure.Exportacion.VentasPorCategoria;

public sealed record GraficosDeVentasPorCategoria(
    ImagenDeGrafico MontoPorCategoria,
    ImagenDeGrafico ParticipacionPorCategoria,
    ImagenDeGrafico? Tendencia,
    ImagenDeGrafico? ProductosMasVendidos)
{
    public static GraficosDeVentasPorCategoria Crear(GeneradorDeGrafico generador, ReporteVentasPorCategoria reporte) =>
        new(
            generador.MontoPorCategoria(reporte.Filas),
            generador.ParticipacionPorCategoria(reporte.Filas),
            CrearTendencia(generador, reporte),
            CrearProductos(generador, reporte));

    private static ImagenDeGrafico? CrearTendencia(GeneradorDeGrafico generador, ReporteVentasPorCategoria reporte) =>
        reporte.Tendencia is { PuedeGraficarse: true } tendencia ? generador.TendenciaDelMonto(tendencia) : null;

    private static ImagenDeGrafico? CrearProductos(GeneradorDeGrafico generador, ReporteVentasPorCategoria reporte) =>
        reporte.ProductosMasVendidos is { Count: > 0 } productos ? generador.ProductosMasVendidos(productos) : null;
}
