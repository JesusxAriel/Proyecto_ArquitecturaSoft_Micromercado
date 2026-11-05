using PuntoMarket.Reportes.Domain.Tendencias;
using PuntoMarket.Reportes.Domain.ValueObjects;
using PuntoMarket.Reportes.Infrastructure.Exportacion.Comunes;
using ScottPlot;

namespace PuntoMarket.Reportes.Infrastructure.Exportacion.Graficos;

internal static class GraficoDeTendencia
{
    private const int MaximoDePuntosEtiquetados = 14;
    private const int Alto = 440;

    public static ImagenDeGrafico Generar(string titulo, TendenciaDeVentas tendencia)
    {
        var grafico = EstiloDeGrafico.NuevoGrafico();
        var fechas = tendencia.Puntos.Select(p => p.Fecha.ToDateTime(TimeOnly.MinValue).ToOADate()).ToArray();
        var montos = tendencia.Puntos.Select(p => (double)p.Monto).ToArray();

        var linea = grafico.Add.Scatter(fechas, montos);
        linea.LineWidth = 4;
        linea.Color = EstiloDeGrafico.Color(EstilosDeReporte.AzulOscuro);
        linea.MarkerSize = 12;
        linea.MarkerFillColor = EstiloDeGrafico.Color(EstilosDeReporte.Ambar);
        linea.MarkerLineColor = EstiloDeGrafico.Color(EstilosDeReporte.AzulOscuro);
        linea.FillY = true;
        linea.FillYColor = EstiloDeGrafico.Color(EstilosDeReporte.AmbarClaro).WithAlpha(0.45);

        EtiquetarPuntos(grafico, tendencia, fechas, montos);
        grafico.Axes.DateTimeTicksBottom();
        FormatearFechas(grafico);
        grafico.Axes.Bottom.Label.FontSize = EstiloDeGrafico.TamanoDeEtiqueta;
        grafico.Axes.Bottom.Label.Text = tendencia.Agrupacion == AgrupacionDeTendencia.Diaria ? "Día" : "Semana (inicio en lunes)";
        grafico.Axes.Left.Label.Text = "Monto (Bs)";
        grafico.Axes.Margins(bottom: 0, top: 0.25);

        var contenido = grafico.GetImageBytes(EstiloDeGrafico.AnchoCompleto, Alto, ImageFormat.Png);
        return new ImagenDeGrafico(titulo, contenido, EstiloDeGrafico.AnchoCompleto, Alto);
    }

    private static void FormatearFechas(Plot grafico)
    {
        grafico.Axes.Bottom.TickLabelStyle.FontSize = EstiloDeGrafico.TamanoDeEtiqueta - 2;
        if (grafico.Axes.Bottom.TickGenerator is ScottPlot.TickGenerators.DateTimeAutomatic fechas)
            fechas.LabelFormatter = fecha => fecha.ToString("dd/MM");
    }

    private static void EtiquetarPuntos(Plot grafico, TendenciaDeVentas tendencia, double[] fechas, double[] montos)
    {
        if (tendencia.Puntos.Count > MaximoDePuntosEtiquetados)
        {
            EtiquetarMaximo(grafico, tendencia, fechas, montos);
            return;
        }

        for (var indice = 0; indice < montos.Length; indice++)
            AgregarEtiqueta(grafico, fechas[indice], montos[indice], tendencia.Puntos[indice].Monto);
    }

    private static void EtiquetarMaximo(Plot grafico, TendenciaDeVentas tendencia, double[] fechas, double[] montos)
    {
        var indice = Array.IndexOf(montos, montos.Max());
        AgregarEtiqueta(grafico, fechas[indice], montos[indice], tendencia.Puntos[indice].Monto);
    }

    private static void AgregarEtiqueta(Plot grafico, double x, double y, decimal monto)
    {
        var texto = grafico.Add.Text(FormatosDeTexto.Moneda(monto), x, y);
        texto.LabelFontSize = EstiloDeGrafico.TamanoDeEtiqueta - 2;
        texto.LabelBold = true;
        texto.LabelFontColor = EstiloDeGrafico.Color(EstilosDeReporte.AzulOscuro);
        texto.OffsetY = -22;
        texto.Alignment = Alignment.LowerCenter;
    }
}
