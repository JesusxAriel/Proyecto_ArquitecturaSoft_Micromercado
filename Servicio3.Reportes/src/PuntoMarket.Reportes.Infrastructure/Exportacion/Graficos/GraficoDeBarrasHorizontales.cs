using PuntoMarket.Reportes.Infrastructure.Exportacion.Comunes;
using ScottPlot;
using ScottPlot.TickGenerators;

namespace PuntoMarket.Reportes.Infrastructure.Exportacion.Graficos;

internal static class GraficoDeBarrasHorizontales
{
    private const int RanurasMinimas = 4;
    private const double MargenVertical = 0.6;

    public static ImagenDeGrafico Generar(string titulo, string ejeDeValores, IReadOnlyList<(string Etiqueta, decimal Valor)> datos, int ancho = EstiloDeGrafico.AnchoMedio)
    {
        var grafico = EstiloDeGrafico.NuevoGrafico();
        var barras = grafico.Add.Bars(CrearBarras(datos));
        barras.Horizontal = true;
        barras.ValueLabelStyle.FontSize = EstiloDeGrafico.TamanoDeEtiqueta;
        barras.ValueLabelStyle.Bold = true;

        grafico.Axes.Left.TickGenerator = new NumericManual(CrearMarcas(datos));
        grafico.Axes.Margins(left: 0, right: 0.4);
        grafico.Axes.SetLimitsY(MinimoEnY(datos.Count), MaximoEnY(datos.Count));
        grafico.Axes.Bottom.Label.Text = ejeDeValores;

        var alto = EstiloDeGrafico.AltoParaBarras(datos.Count, ancho);
        var contenido = grafico.GetImageBytes(ancho, alto, ImageFormat.Png);
        return new ImagenDeGrafico(titulo, contenido, ancho, alto);
    }

    private static double HolguraEnY(int cantidad) => Math.Max(0, (RanurasMinimas - cantidad) / 2.0);

    private static double MinimoEnY(int cantidad) => -HolguraEnY(cantidad) - MargenVertical;

    private static double MaximoEnY(int cantidad) => cantidad - 1 + HolguraEnY(cantidad) + MargenVertical;

    private static Bar[] CrearBarras(IReadOnlyList<(string Etiqueta, decimal Valor)> datos) =>
        datos
            .Select((dato, indice) => new Bar
            {
                Position = datos.Count - 1 - indice,
                Value = (double)dato.Valor,
                FillColor = EstiloDeGrafico.Color(indice % 2 == 0 ? EstilosDeReporte.AzulOscuro : EstilosDeReporte.Ambar),
                LineColor = EstiloDeGrafico.Color(EstilosDeReporte.AzulOscuro),
                LineWidth = 1,
                Label = FormatosDeTexto.Moneda(dato.Valor),
                Size = 0.62
            })
            .ToArray();

    private static Tick[] CrearMarcas(IReadOnlyList<(string Etiqueta, decimal Valor)> datos) =>
        datos
            .Select((dato, indice) => new Tick(
                datos.Count - 1 - indice,
                FormatosDeTexto.Abreviar(dato.Etiqueta, EstiloDeGrafico.MaximoDeCaracteres)))
            .ToArray();
}
