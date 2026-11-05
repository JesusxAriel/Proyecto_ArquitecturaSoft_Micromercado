using PuntoMarket.Reportes.Domain.ValueObjects;
using PuntoMarket.Reportes.Infrastructure.Exportacion.Comunes;
using ScottPlot;

namespace PuntoMarket.Reportes.Infrastructure.Exportacion.Graficos;

internal static class GraficoDeDona
{
    private const int AltoDeLaDona = 560;
    private const int AltoPorLeyenda = 38;
    private const int AltoCompacto = 440;
    private const double UnidadesVisibles = 3.0;
    private const double UnidadesDelAltoCompacto = 3.1;
    private const double LimiteIzquierdoCompacto = -1.4;
    private const double FraccionDelCentro = 0.52;
    private const int CaracteresDeLeyendaCompacta = 24;

    public static ImagenDeGrafico Generar(string titulo, IReadOnlyList<ParteDelTotal> partes)
    {
        var alto = AltoSegunPartes(partes.Count);
        var grafico = CrearDona(partes, EstiloDeGrafico.MaximoDeCaracteres);
        grafico.ShowLegend(Alignment.LowerCenter, Orientation.Vertical);
        FijarLimitesVerticales(grafico, alto);

        return Renderizar(titulo, grafico, alto);
    }

    public static ImagenDeGrafico GenerarConLeyendaLateral(string titulo, IReadOnlyList<ParteDelTotal> partes)
    {
        var grafico = CrearDona(partes, CaracteresDeLeyendaCompacta);
        grafico.ShowLegend(Alignment.MiddleRight, Orientation.Vertical);
        var pixelesPorUnidad = AltoCompacto / UnidadesDelAltoCompacto;
        var unidadesEnAncho = EstiloDeGrafico.AnchoMedio / pixelesPorUnidad;
        grafico.Axes.SetLimits(
            LimiteIzquierdoCompacto,
            LimiteIzquierdoCompacto + unidadesEnAncho,
            -UnidadesDelAltoCompacto / 2,
            UnidadesDelAltoCompacto / 2);

        return Renderizar(titulo, grafico, AltoCompacto);
    }

    private static Plot CrearDona(IReadOnlyList<ParteDelTotal> partes, int maximoDeCaracteres)
    {
        var total = partes.Sum(p => p.Monto);
        var grafico = EstiloDeGrafico.NuevoGrafico();
        var dona = grafico.Add.Pie(CrearPorciones(partes, total, maximoDeCaracteres));
        dona.DonutFraction = FraccionDelCentro;
        dona.SliceLabelDistance = 0.78;

        grafico.Axes.Frameless();
        grafico.HideGrid();
        grafico.Legend.FontSize = EstiloDeGrafico.TamanoDeEtiqueta;
        grafico.Legend.BackgroundColor = EstiloDeGrafico.Color(EstilosDeReporte.Blanco);
        grafico.Legend.OutlineColor = EstiloDeGrafico.Color(EstilosDeReporte.Borde);
        return grafico;
    }

    private static ImagenDeGrafico Renderizar(string titulo, Plot grafico, int alto)
    {
        var contenido = grafico.GetImageBytes(EstiloDeGrafico.AnchoMedio, alto, ImageFormat.Png);
        return new ImagenDeGrafico(titulo, contenido, EstiloDeGrafico.AnchoMedio, alto);
    }

    private static int AltoSegunPartes(int cantidad) => AltoDeLaDona + AltoPorLeyenda * cantidad;

    private static void FijarLimitesVerticales(Plot grafico, int alto)
    {
        var pixelesPorUnidad = EstiloDeGrafico.AnchoMedio / UnidadesVisibles;
        var unidadesEnAlto = alto / pixelesPorUnidad;
        const double margenSuperior = 1.1;
        grafico.Axes.SetLimits(-UnidadesVisibles / 2, UnidadesVisibles / 2, margenSuperior - unidadesEnAlto, margenSuperior);
    }

    private static List<PieSlice> CrearPorciones(IReadOnlyList<ParteDelTotal> partes, decimal total, int maximoDeCaracteres) =>
        partes
            .Select((parte, indice) => new PieSlice
            {
                Value = (double)parte.Monto,
                FillColor = EstiloDeGrafico.ColorDeSerie(indice),
                Label = FormatosDeTexto.Porcentaje(parte.Monto, total),
                LabelFontSize = EstiloDeGrafico.TamanoDeEtiqueta,
                LabelBold = true,
                LabelFontColor = EtiquetaLegible(indice),
                LegendText = TextoDeLeyenda(parte, maximoDeCaracteres)
            })
            .ToList();

    private static string TextoDeLeyenda(ParteDelTotal parte, int maximoDeCaracteres) =>
        maximoDeCaracteres == CaracteresDeLeyendaCompacta
            ? FormatosDeTexto.Abreviar(parte.Nombre, maximoDeCaracteres)
            : $"{FormatosDeTexto.Abreviar(parte.Nombre, maximoDeCaracteres)} - {FormatosDeTexto.Bolivianos(parte.Monto)}";

    private static Color EtiquetaLegible(int indice) =>
        EstiloDeGrafico.Color(indice % EstilosDeReporte.SerieDeGraficos.Count is 0 or 3 or 2
            ? EstilosDeReporte.Blanco
            : EstilosDeReporte.AzulOscuro);
}
