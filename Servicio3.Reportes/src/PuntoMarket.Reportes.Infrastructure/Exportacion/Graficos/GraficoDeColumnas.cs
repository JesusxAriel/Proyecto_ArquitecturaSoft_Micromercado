using PuntoMarket.Reportes.Domain.ValueObjects;
using PuntoMarket.Reportes.Infrastructure.Exportacion.Comunes;
using ScottPlot;
using ScottPlot.TickGenerators;

namespace PuntoMarket.Reportes.Infrastructure.Exportacion.Graficos;

internal static class GraficoDeColumnas
{
    private const int MaximoDeColumnasEtiquetadas = 8;
    private const int MaximoDeMarcas = 8;
    private const int Alto = 440;

    public static ImagenDeGrafico Generar(string titulo, string ejeHorizontal, IReadOnlyList<PuntoDeTendencia> puntos)
    {
        var grafico = EstiloDeGrafico.NuevoGrafico();
        var columnas = grafico.Add.Bars(CrearColumnas(puntos));
        columnas.ValueLabelStyle.ForeColor = EstiloDeGrafico.Color(EstilosDeReporte.AzulOscuro);
        columnas.ValueLabelStyle.FontSize = EstiloDeGrafico.TamanoDeEtiqueta - 3;
        columnas.ValueLabelStyle.Bold = true;

        grafico.Axes.Bottom.TickGenerator = new NumericManual(CrearMarcas(puntos));
        grafico.Axes.Bottom.Label.Text = ejeHorizontal;
        grafico.Axes.Left.Label.Text = "Monto (Bs)";
        grafico.Axes.Margins(bottom: 0, top: 0.22);

        var contenido = grafico.GetImageBytes(EstiloDeGrafico.AnchoMedio, Alto, ImageFormat.Png);
        return new ImagenDeGrafico(titulo, contenido, EstiloDeGrafico.AnchoMedio, Alto);
    }

    private static Bar[] CrearColumnas(IReadOnlyList<PuntoDeTendencia> puntos) =>
        puntos
            .Select((punto, indice) => new Bar
            {
                Position = indice,
                Value = (double)punto.Monto,
                FillColor = EstiloDeGrafico.Color(EstilosDeReporte.Ambar),
                LineColor = EstiloDeGrafico.Color(EstilosDeReporte.AzulOscuro),
                LineWidth = 1,
                Label = DebeEtiquetar(puntos, punto) ? FormatosDeTexto.Moneda(punto.Monto) : string.Empty,
                Size = 0.7
            })
            .ToArray();

    private static bool DebeEtiquetar(IReadOnlyList<PuntoDeTendencia> puntos, PuntoDeTendencia punto) =>
        puntos.Count <= MaximoDeColumnasEtiquetadas || punto.Monto == puntos.Max(p => p.Monto);

    private static Tick[] CrearMarcas(IReadOnlyList<PuntoDeTendencia> puntos) =>
        puntos
            .Select((punto, indice) => new Tick(indice, punto.Fecha.ToString("dd/MM")))
            .Where((marca, indice) => indice % PasoDeMarcas(puntos.Count) == 0)
            .ToArray();

    private static int PasoDeMarcas(int cantidad) => (int)Math.Ceiling(cantidad / (double)MaximoDeMarcas);
}
