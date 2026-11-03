using System.Globalization;
using PuntoMarket.Reportes.Domain.VentasPorCategoria;

namespace PuntoMarket.Reportes.Infrastructure.Exportacion.VentasPorCategoria;

public static class TextosDelReporte
{
    public const string Titulo = "Ventas por categoría";
    public const string SinResultados = "Sin resultados para los filtros seleccionados";

    private static readonly CultureInfo Cultura = ObtenerCultura();

    public static string Moneda(decimal monto) => monto.ToString("N2", Cultura);

    public static string Entero(long valor) => valor.ToString("N0", Cultura);

    public static string Porcentaje(decimal monto, decimal total) =>
        total == 0 ? "0,0 %" : (monto / total).ToString("P1", Cultura);

    public static string FechaYHora(DateTime fecha) => fecha.ToString("dd/MM/yyyy HH:mm", Cultura);

    public static string Periodo(FiltrosDeVentasPorCategoria filtros) =>
        (filtros.Rango.Desde, filtros.Rango.Hasta) switch
        {
            (null, null) => "Todas las fechas",
            ({ } desde, null) => $"Desde el {Fecha(desde)}",
            (null, { } hasta) => $"Hasta el {Fecha(hasta)}",
            ({ } desde, { } hasta) => $"Del {Fecha(desde)} al {Fecha(hasta)}"
        };

    public static string Categoria(ReporteVentasPorCategoria reporte) =>
        reporte.Filtros.IdCategoria is { } id
            ? reporte.Filas.FirstOrDefault()?.NombreCategoria ?? $"N.º {id}"
            : "Todas las categorías";

    public static string Limite(FiltrosDeVentasPorCategoria filtros) =>
        filtros.Top is { } top ? $"Las {top} con mayor monto" : "Sin límite";

    public static string Lider(TotalesPorCategoria totales) =>
        totales.CategoriaLider?.NombreCategoria ?? "No aplica";

    private static string Fecha(DateOnly fecha) => fecha.ToString("dd/MM/yyyy", Cultura);

    private static CultureInfo ObtenerCultura()
    {
        try
        {
            return CultureInfo.GetCultureInfo("es-BO");
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.InvariantCulture;
        }
    }
}
