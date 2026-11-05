using PuntoMarket.Reportes.Domain.ValueObjects;
using PuntoMarket.Reportes.Infrastructure.Exportacion.Comunes;
using ScottPlot;

namespace PuntoMarket.Reportes.Infrastructure.Exportacion.Graficos;

internal static class GraficoDeDona
{
    private const int AltoDeLaDona = 560;
    private const int AltoPorLeyenda = 38;
    private const double UnidadesVisibles = 3.0;
    private const double FraccionDelCentro = 0.52;

    public static ImagenDeGrafico Generar(string titulo, IReadOnlyList<ParteDelTotal> partes)
    {
        var total = partes.Sum(p => p.Monto);
        var grafico = EstiloDeGrafico.NuevoGrafico();
        var dona = grafico.Add.Pie(CrearPorciones(partes, total));
        dona.DonutFraction = FraccionDelCentro;
        dona.SliceLabelDistance = 0.78;

        grafico.Axes.Frameless();
        grafico.HideGrid();
        grafico.Legend.FontSize = EstiloDeGrafico.TamanoDeEtiqueta;
        grafico.Legend.BackgroundColor = EstiloDeGrafico.Color(EstilosDeReporte.Blanco);
        grafico.Legend.OutlineColor = EstiloDeGrafico.Color(EstilosDeReporte.Borde);
        grafico.ShowLegend(Alignment.LowerCenter, Orientation.Vertical);

        var alto = AltoSegunPartes(partes.Count);
        FijarLimites(grafico, alto);
        var contenido = grafico.GetImageBytes(EstiloDeGrafico.AnchoMedio, alto, ImageFormat.Png);
        return new ImagenDeGrafico(titulo, contenido, EstiloDeGrafico.AnchoMedio, alto);
    }

    private static int AltoSegunPartes(int cantidad) => AltoDeLaDona + AltoPorLeyenda * cantidad;

    private static void FijarLimites(Plot grafico, int alto)
    {
        var pixelesPorUnidad = EstiloDeGrafico.AnchoMedio / UnidadesVisibles;
        var unidadesEnAlto = alto / pixelesPorUnidad;
        const double margenSuperior = 1.1;
        grafico.Axes.SetLimits(-UnidadesVisibles / 2, UnidadesVisibles / 2, margenSuperior - unidadesEnAlto, margenSuperior);
    }

    private static List<PieSlice> CrearPorciones(IReadOnlyList<ParteDelTotal> partes, decimal total) =>
        partes
            .Select((parte, indice) => new PieSlice
            {
                Value = (double)parte.Monto,
                FillColor = EstiloDeGrafico.ColorDeSerie(indice),
                Label = FormatosDeTexto.Porcentaje(parte.Monto, total),
                LabelFontSize = EstiloDeGrafico.TamanoDeEtiqueta,
                LabelBold = true,
                LabelFontColor = EtiquetaLegible(indice),
                LegendText = $"{FormatosDeTexto.Abreviar(parte.Nombre, EstiloDeGrafico.MaximoDeCaracteres)} - {FormatosDeTexto.Bolivianos(parte.Monto)}"
            })
            .ToList();

    private static Color EtiquetaLegible(int indice) =>
        EstiloDeGrafico.Color(indice % EstilosDeReporte.SerieDeGraficos.Count is 0 or 3 or 2
            ? EstilosDeReporte.Blanco
            : EstilosDeReporte.AzulOscuro);
}
