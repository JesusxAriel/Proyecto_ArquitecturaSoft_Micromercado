using PuntoMarket.Reportes.Infrastructure.Exportacion.Comunes;
using ScottPlot;

namespace PuntoMarket.Reportes.Infrastructure.Exportacion.Graficos;

internal static class EstiloDeGrafico
{
    public const int AnchoMedio = 760;
    public const int AnchoCompleto = 1560;
    public const int AltoMinimo = 360;
    public const int AltoMinimoDeBarrasAnchas = 380;
    public const int AltoMinimoDeBarrasEstrechas = 560;
    public const int AltoPorBarra = 52;
    public const int AltoFijoDeBarras = 130;
    public const int TamanoDeEtiqueta = 22;
    public const int TamanoDeTitulo = 24;
    public const int MaximoDeCaracteres = 28;

    public static Color Color(string html) => ScottPlot.Color.FromHex(html);

    public static Color ColorDeSerie(int indice) => Color(EstilosDeReporte.ColorDeSerie(indice));

    public static Plot NuevoGrafico()
    {
        var grafico = new Plot();
        grafico.FigureBackground.Color = Color(EstilosDeReporte.Blanco);
        grafico.DataBackground.Color = Color(EstilosDeReporte.Blanco);
        grafico.Grid.MajorLineColor = Color(EstilosDeReporte.Borde);
        grafico.Axes.Bottom.TickLabelStyle.FontSize = TamanoDeEtiqueta;
        grafico.Axes.Left.TickLabelStyle.FontSize = TamanoDeEtiqueta;
        grafico.Axes.Bottom.Label.FontSize = TamanoDeEtiqueta;
        grafico.Axes.Left.Label.FontSize = TamanoDeEtiqueta;
        return grafico;
    }

    public static int AltoParaBarras(int cantidad, int ancho) =>
        Math.Max(
            ancho >= AnchoCompleto ? AltoMinimoDeBarrasAnchas : AltoMinimoDeBarrasEstrechas,
            AltoFijoDeBarras + AltoPorBarra * cantidad);
}
