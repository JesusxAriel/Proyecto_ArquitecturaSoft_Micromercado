using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PuntoMarket.Reportes.Infrastructure.Exportacion.Comunes;

public static class ComponentesDePdf
{
    private const int TamanoDelLogo = 56;
    private const int SeparacionDeTarjetas = 8;

    public static void Encabezado(IContainer contenedor, byte[] logo, string titulo, string subtitulo) =>
        contenedor
            .Background(EstilosDeReporte.AzulOscuro)
            .BorderBottom(4).BorderColor(EstilosDeReporte.Ambar)
            .Padding(10)
            .Row(fila =>
            {
                fila.ConstantItem(TamanoDelLogo).Height(TamanoDelLogo).Image(logo).FitArea();
                fila.RelativeItem().PaddingLeft(12).AlignMiddle().Column(columna =>
                {
                    columna.Item().Text("PuntoMarket").FontSize(10).FontColor(EstilosDeReporte.Ambar).Bold();
                    columna.Item().Text(titulo).FontSize(22).FontColor(EstilosDeReporte.Blanco).Bold();
                    columna.Item().Text(subtitulo).FontSize(9).FontColor(EstilosDeReporte.AmbarClaro);
                });
            });

    public static void Filtros(IContainer contenedor, IEnumerable<string> lineas) =>
        contenedor.Column(columna =>
        {
            columna.Item().Text("Filtros aplicados").Bold().FontSize(11).FontColor(EstilosDeReporte.AmbarOscuro);
            foreach (var linea in lineas)
                columna.Item().Text(linea).FontSize(9);
        });

    public static void Tarjetas(IContainer contenedor, IReadOnlyList<Indicador> indicadores) =>
        contenedor.Row(fila =>
        {
            fila.Spacing(SeparacionDeTarjetas);
            foreach (var indicador in indicadores)
                fila.RelativeItem().Element(c => Tarjeta(c, indicador));
        });

    public static void TarjetaDeGrafico(IContainer contenedor, ImagenDeGrafico grafico) =>
        contenedor
            .Border(1).BorderColor(EstilosDeReporte.Borde)
            .Padding(6)
            .Column(columna =>
            {
                columna.Item().PaddingBottom(4).Text(grafico.Titulo).Bold().FontSize(10).FontColor(EstilosDeReporte.AzulGrisaceo);
                columna.Item().Image(grafico.Contenido).FitWidth();
            });

    public static void Pie(IContainer contenedor, string sumariados, string reporteGenerado) =>
        contenedor
            .BorderTop(2).BorderColor(EstilosDeReporte.Ambar)
            .PaddingTop(6)
            .Column(columna =>
            {
                columna.Item().Text(sumariados).FontSize(9).Bold();
                columna.Item().Row(fila =>
                {
                    fila.RelativeItem().Text(reporteGenerado).FontSize(8);
                    fila.ConstantItem(120).AlignRight().DefaultTextStyle(estilo => estilo.FontSize(8)).Text(NumeroDePagina);
                });
            });

    public static IContainer CeldaDeEncabezado(IContainer contenedor) =>
        contenedor
            .Background(EstilosDeReporte.AzulGrisaceo)
            .Padding(5)
            .DefaultTextStyle(estilo => estilo.FontColor(EstilosDeReporte.Blanco).Bold().FontSize(9));

    public static IContainer CeldaDeFila(IContainer contenedor, int indice) =>
        contenedor
            .Background(indice % 2 == 0 ? EstilosDeReporte.Blanco : EstilosDeReporte.FondoSuave)
            .Padding(5)
            .DefaultTextStyle(estilo => estilo.FontSize(9));

    public static IContainer CeldaDeTotales(IContainer contenedor) =>
        contenedor
            .Background(EstilosDeReporte.Ambar)
            .BorderTop(1).BorderColor(EstilosDeReporte.AzulOscuro)
            .Padding(5)
            .DefaultTextStyle(estilo => estilo.FontColor(EstilosDeReporte.AzulOscuro).Bold().FontSize(9));

    private static void Tarjeta(IContainer contenedor, Indicador indicador) =>
        contenedor
            .Background(EstilosDeReporte.FondoSuave)
            .BorderLeft(4).BorderColor(EstilosDeReporte.Ambar)
            .Padding(8)
            .Column(columna =>
            {
                columna.Item().Text(indicador.Etiqueta).FontSize(8).FontColor(EstilosDeReporte.TextoAtenuado).Bold();
                columna.Item().Text(indicador.Valor).FontSize(15).FontColor(EstilosDeReporte.AzulOscuro).Bold();
                if (indicador.Detalle is { } detalle)
                    columna.Item().Text(detalle).FontSize(8).FontColor(EstilosDeReporte.AzulGrisaceo);
            });

    private static void NumeroDePagina(TextDescriptor texto)
    {
        texto.Span("Página ");
        texto.CurrentPageNumber();
        texto.Span(" de ");
        texto.TotalPages();
    }
}
