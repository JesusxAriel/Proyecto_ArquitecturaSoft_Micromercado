using PuntoMarket.Reportes.Domain.VentasPorCategoria;
using PuntoMarket.Reportes.Infrastructure.Exportacion.Comunes;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PuntoMarket.Reportes.Infrastructure.Exportacion.VentasPorCategoria;

public class VentasPorCategoriaDocumento : IDocument
{
    private const string FondoFilaAlterna = "#F1F5F9";
    private const int MargenDePagina = 30;

    private readonly ReporteVentasPorCategoria _reporte;
    private readonly byte[]? _grafico;
    private readonly byte[] _logo;

    public VentasPorCategoriaDocumento(ReporteVentasPorCategoria reporte, byte[]? grafico, byte[] logo)
    {
        _reporte = reporte;
        _grafico = grafico;
        _logo = logo;
    }

    public DocumentMetadata GetMetadata() => new() { Title = TextosDelReporte.Titulo };

    public void Compose(IDocumentContainer documento) =>
        documento.Page(pagina =>
        {
            pagina.Size(PageSizes.A4);
            pagina.Margin(MargenDePagina);
            pagina.DefaultTextStyle(estilo => estilo.FontSize(10).FontColor(EstilosDeReporte.AzulOscuro));
            pagina.Header().Element(ComponerEncabezado);
            pagina.Content().PaddingVertical(12).Element(ComponerContenido);
            pagina.Footer().Element(ComponerPie);
        });

    private void ComponerEncabezado(IContainer contenedor) =>
        contenedor
            .Background(EstilosDeReporte.AzulOscuro)
            .BorderBottom(4).BorderColor(EstilosDeReporte.Ambar)
            .Padding(10)
            .Row(fila =>
            {
                fila.ConstantItem(56).Height(56).Image(_logo).FitArea();
                fila.RelativeItem().PaddingLeft(12).AlignMiddle().Column(columna =>
                {
                    columna.Item().Text("PuntoMarket").FontSize(10).FontColor(EstilosDeReporte.Ambar).Bold();
                    columna.Item().Text(TextosDelReporte.Titulo).FontSize(22).FontColor(EstilosDeReporte.Blanco).Bold();
                });
            });

    private void ComponerContenido(IContainer contenedor) =>
        contenedor.Column(columna =>
        {
            columna.Spacing(12);
            columna.Item().Element(ComponerFiltros);
            columna.Item().Element(ComponerCuerpo);
        });

    private void ComponerFiltros(IContainer contenedor) =>
        contenedor.Column(columna =>
        {
            columna.Item().Text("Filtros aplicados").Bold().FontSize(12).FontColor(EstilosDeReporte.AmbarOscuro);
            columna.Item().Text($"Período: {TextosDelReporte.Periodo(_reporte.Filtros)}");
            columna.Item().Text($"Categoría: {TextosDelReporte.Categoria(_reporte)}");
            columna.Item().Text($"Cantidad de categorías: {TextosDelReporte.Limite(_reporte.Filtros)}");
        });

    private void ComponerCuerpo(IContainer contenedor)
    {
        if (_reporte.Filas.Count == 0)
        {
            contenedor.PaddingVertical(30).AlignCenter().Text(TextosDelReporte.SinResultados).FontSize(13).Italic();
            return;
        }

        contenedor.Column(columna =>
        {
            columna.Spacing(16);
            columna.Item().Element(ComponerTabla);
            columna.Item().Element(ComponerGrafico);
        });
    }

    private void ComponerTabla(IContainer contenedor) =>
        contenedor.Table(tabla =>
        {
            DefinirColumnas(tabla);
            tabla.Header(ComponerEncabezadoDeTabla);
            for (var indice = 0; indice < _reporte.Filas.Count; indice++)
                ComponerFila(tabla, _reporte.Filas[indice], indice);
        });

    private static void DefinirColumnas(TableDescriptor tabla) =>
        tabla.ColumnsDefinition(columnas =>
        {
            columnas.ConstantColumn(28);
            columnas.RelativeColumn(3);
            columnas.RelativeColumn(1.4f);
            columnas.RelativeColumn(1.6f);
            columnas.RelativeColumn(1.2f);
        });

    private static void ComponerEncabezadoDeTabla(TableCellDescriptor encabezado)
    {
        encabezado.Cell().Element(CeldaDeEncabezado).Text("N.º");
        encabezado.Cell().Element(CeldaDeEncabezado).Text("Categoría");
        encabezado.Cell().Element(CeldaDeEncabezado).AlignRight().Text("Unidades vendidas");
        encabezado.Cell().Element(CeldaDeEncabezado).AlignRight().Text("Monto vendido (Bs)");
        encabezado.Cell().Element(CeldaDeEncabezado).AlignRight().Text("Participación");
    }

    private void ComponerFila(TableDescriptor tabla, FilaDeCategoria fila, int indice)
    {
        var fondo = indice % 2 == 0 ? EstilosDeReporte.Blanco : FondoFilaAlterna;
        IContainer Celda(IContainer c) => c.Background(fondo).Padding(5);

        tabla.Cell().Element(Celda).Text((indice + 1).ToString());
        tabla.Cell().Element(Celda).Text(fila.NombreCategoria);
        tabla.Cell().Element(Celda).AlignRight().Text(TextosDelReporte.Entero(fila.UnidadesVendidas));
        tabla.Cell().Element(Celda).AlignRight().Text(TextosDelReporte.Moneda(fila.MontoVendido));
        tabla.Cell().Element(Celda).AlignRight()
            .Text(TextosDelReporte.Porcentaje(fila.MontoVendido, _reporte.Totales.TotalMonto));
    }

    private void ComponerGrafico(IContainer contenedor)
    {
        if (_grafico is null)
            return;

        contenedor.Image(_grafico).FitWidth();
    }

    private void ComponerPie(IContainer contenedor) =>
        contenedor
            .BorderTop(2).BorderColor(EstilosDeReporte.Ambar)
            .PaddingTop(6)
            .Column(columna =>
            {
                columna.Item().DefaultTextStyle(estilo => estilo.FontSize(9).Bold()).Text(ComponerSumariados);
                columna.Item().Row(fila =>
                {
                    fila.RelativeItem().Text(
                        FormatosDeTexto.ReporteGenerado(_reporte.GeneradoPor, _reporte.GeneradoEn))
                        .FontSize(8);
                    fila.ConstantItem(120).AlignRight().DefaultTextStyle(estilo => estilo.FontSize(8)).Text(ComponerNumeroDePagina);
                });
            });

    private void ComponerSumariados(TextDescriptor texto)
    {
        var totales = _reporte.Totales;
        texto.Span($"Total de unidades: {TextosDelReporte.Entero(totales.TotalUnidades)}   |   ");
        texto.Span($"Monto total: Bs {TextosDelReporte.Moneda(totales.TotalMonto)}   |   ");
        texto.Span($"Categoría líder: {TextosDelReporte.Lider(totales)}");
    }

    private static void ComponerNumeroDePagina(TextDescriptor texto)
    {
        texto.Span("Página ");
        texto.CurrentPageNumber();
        texto.Span(" de ");
        texto.TotalPages();
    }

    private static IContainer CeldaDeEncabezado(IContainer contenedor) =>
        contenedor
            .Background(EstilosDeReporte.AzulGrisaceo)
            .Padding(5)
            .DefaultTextStyle(estilo => estilo.FontColor(EstilosDeReporte.Blanco).Bold().FontSize(9));
}
