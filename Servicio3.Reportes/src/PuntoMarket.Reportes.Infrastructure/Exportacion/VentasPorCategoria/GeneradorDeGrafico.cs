using PuntoMarket.Reportes.Domain.VentasPorCategoria;
using PuntoMarket.Reportes.Infrastructure.Exportacion.Comunes;
using ScottPlot;
using ScottPlot.TickGenerators;

namespace PuntoMarket.Reportes.Infrastructure.Exportacion.VentasPorCategoria;

public class GeneradorDeGrafico
{
    private const int Ancho = 900;
    private const int AltoMinimo = 260;
    private const int AltoPorCategoria = 42;
    private const int AltoFijo = 120;

    public byte[] GenerarBarras(IReadOnlyList<FilaDeCategoria> filas)
    {
        var grafico = new Plot();
        var barras = CrearBarras(filas);

        ConfigurarBarras(grafico.Add.Bars(barras));
        ConfigurarEjes(grafico, filas);

        return grafico.GetImageBytes(Ancho, CalcularAlto(filas.Count), ImageFormat.Png);
    }

    private static Bar[] CrearBarras(IReadOnlyList<FilaDeCategoria> filas) =>
        filas
            .Select((fila, indice) => new Bar
            {
                Position = filas.Count - 1 - indice,
                Value = (double)fila.MontoVendido,
                FillColor = Color.FromHex(EstilosDeReporte.Ambar),
                Size = 0.6
            })
            .ToArray();

    private static void ConfigurarBarras(ScottPlot.Plottables.BarPlot barras)
    {
        barras.Horizontal = true;
    }

    private static void ConfigurarEjes(Plot grafico, IReadOnlyList<FilaDeCategoria> filas)
    {
        grafico.Axes.Left.TickGenerator = new NumericManual(CrearEtiquetas(filas));
        grafico.Axes.Margins(left: 0, right: 0.12);
        grafico.Title("Monto vendido por categoría");
        grafico.XLabel("Monto vendido (Bs)");
        grafico.FigureBackground.Color = Color.FromHex(EstilosDeReporte.Blanco);
    }

    private static Tick[] CrearEtiquetas(IReadOnlyList<FilaDeCategoria> filas) =>
        filas
            .Select((fila, indice) => new Tick(filas.Count - 1 - indice, fila.NombreCategoria))
            .ToArray();

    private static int CalcularAlto(int cantidadDeCategorias) =>
        Math.Max(AltoMinimo, AltoFijo + AltoPorCategoria * cantidadDeCategorias);
}
