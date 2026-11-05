using System.Globalization;
using PuntoMarket.Ventas.Frontend.Clients;
namespace PuntoMarket.Ventas.Frontend.Pages.Reportes;

public sealed record IndicadorReporte(string Etiqueta, string Valor, string? Detalle = null)
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-BO");

    public static IReadOnlyList<IndicadorReporte> DeListado(ListadoVentasReporte reporte) =>
    [
        new("Ventas completadas", reporte.Totales.VentasCompletadas.ToString("N0", Cultura)),
        new("Monto total", Bolivianos(reporte.Totales.MontoTotalDeCompletadas)),
        new("Promedio por venta", Bolivianos(reporte.Totales.PromedioPorVenta)),
        new("Venta más alta", Bolivianos(reporte.Totales.VentaMasAlta)),
        new("Ventas anuladas", reporte.Totales.VentasAnuladas.ToString("N0", Cultura))
    ];

    public static IReadOnlyList<IndicadorReporte> DeCategorias(CategoriasReporte reporte) =>
    [
        new("Monto total", Bolivianos(reporte.Totales.TotalMonto)),
        new("Unidades vendidas", reporte.Totales.TotalUnidades.ToString("N0", Cultura)),
        new("Categorías", reporte.Filas.Count.ToString("N0", Cultura)),
        new("Categoría líder", reporte.Totales.CategoriaLider ?? "Sin datos", DetalleDelLider(reporte))
    ];

    private static string Bolivianos(decimal monto) => "Bs " + monto.ToString("N2", Cultura);

    private static string? DetalleDelLider(CategoriasReporte reporte)
    {
        var lider = reporte.Filas.MaxBy(f => f.MontoVendido);
        if (lider is null || reporte.Totales.TotalMonto == 0m) return null;

        return $"{Bolivianos(lider.MontoVendido)} ({(lider.MontoVendido / reporte.Totales.TotalMonto).ToString("P1", Cultura)})";
    }
}
