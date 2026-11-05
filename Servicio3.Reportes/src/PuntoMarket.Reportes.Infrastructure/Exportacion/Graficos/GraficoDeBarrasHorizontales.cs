using PuntoMarket.Reportes.Infrastructure.Exportacion.Comunes;
using ScottPlot;
using ScottPlot.TickGenerators;

namespace PuntoMarket.Reportes.Infrastructure.Exportacion.Graficos;

internal static class GraficoDeBarrasHorizontales
{
    public static ImagenDeGrafico Generar(string titulo, string ejeDeValores, IReadOnlyList<(string Etiqueta, decimal Valor)> datos)
    {
        var grafico = EstiloDeGrafico.NuevoGrafico();
        var barras = grafico.Add.Bars(CrearBarras(datos));
        barras.Horizontal = true;
        barras.ValueLabelStyle.FontSize = EstiloDeGrafico.TamanoDeEtiqueta;
        barras.ValueLabelStyle.Bold = true;

        grafico.Axes.Left.TickGenerator = new NumericManual(CrearMarcas(datos));
        grafico.Axes.Margins(left: 0, right: 0.28);
        grafico.Axes.Bottom.Label.Text = ejeDeValores;

        var alto = EstiloDeGrafico.AltoParaBarras(datos.Count);
        var contenido = grafico.GetImageBytes(EstiloDeGrafico.AnchoMedio, alto, ImageFormat.Png);
        return new ImagenDeGrafico(titulo, contenido, EstiloDeGrafico.AnchoMedio, alto);
    }

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
