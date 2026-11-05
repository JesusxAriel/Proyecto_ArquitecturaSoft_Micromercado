using PuntoMarket.Reportes.Domain.ListadoVentas;
using PuntoMarket.Reportes.Infrastructure.Exportacion.Comunes;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PuntoMarket.Reportes.Infrastructure.Exportacion.ListadoVentas;

public class ListadoVentasDocumento : IDocument
{
    private const string FondoFilaAlterna = "#F1F5F9";
    private const int MargenDePagina = 30;

    private readonly ReporteListadoVentas _reporte;
    private readonly byte[] _logo;

    public ListadoVentasDocumento(ReporteListadoVentas reporte, byte[] logo)
    {
        _reporte = reporte;
        _logo = logo;
    }

    public DocumentMetadata GetMetadata() => new() { Title = TextosDelListadoVentas.Titulo };

    public void Compose(IDocumentContainer documento) =>
        documento.Page(pagina =>
        {
            pagina.Size(PageSizes.A4.Landscape());
            pagina.Margin(MargenDePagina);
            pagina.DefaultTextStyle(estilo => estilo.FontSize(9).FontColor(EstilosDeReporte.AzulOscuro));
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
                    columna.Item().Text(TextosDelListadoVentas.Titulo).FontSize(22).FontColor(EstilosDeReporte.Blanco).Bold();
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
            columna.Item().Text($"Período: {FormatosDeTexto.Periodo(_reporte.Filtros.Rango)}");
            columna.Item().Text($"Estado: {TextosDelListadoVentas.EstadoFiltrado(_reporte.Filtros)}");
            columna.Item().Text($"Cliente: {TextosDelListadoVentas.ClienteFiltrado(_reporte.Filtros)}");
            columna.Item().Text($"Cantidad de ventas: {TextosDelListadoVentas.Limite(_reporte.Filtros)}");
        });

    private void ComponerCuerpo(IContainer contenedor)
    {
        if (_reporte.Filas.Count == 0)
        {
            contenedor.PaddingVertical(30).AlignCenter().Text(FormatosDeTexto.SinResultados).FontSize(13).Italic();
            return;
        }

        contenedor.Element(ComponerTabla);
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
            columnas.ConstantColumn(26);
            columnas.RelativeColumn(1.6f);
            columnas.RelativeColumn(1.6f);
            columnas.RelativeColumn(1.3f);
            columnas.RelativeColumn(3);
            columnas.RelativeColumn(1.4f);
            columnas.RelativeColumn(1.2f);
            columnas.RelativeColumn(1.5f);
        });

    private static void ComponerEncabezadoDeTabla(TableCellDescriptor encabezado)
    {
        encabezado.Cell().Element(CeldaDeEncabezado).Text("N.º");
        encabezado.Cell().Element(CeldaDeEncabezado).Text("Comprobante");
        encabezado.Cell().Element(CeldaDeEncabezado).Text("Fecha y hora");
        encabezado.Cell().Element(CeldaDeEncabezado).Text("NIT/CI");
        encabezado.Cell().Element(CeldaDeEncabezado).Text("Razón social");
        encabezado.Cell().Element(CeldaDeEncabezado).Text("Cajero");
        encabezado.Cell().Element(CeldaDeEncabezado).Text("Estado");
        encabezado.Cell().Element(CeldaDeEncabezado).AlignRight().Text("Monto (Bs)");
    }

    private void ComponerFila(TableDescriptor tabla, FilaDeVenta fila, int indice)
    {
        var fondo = indice % 2 == 0 ? EstilosDeReporte.Blanco : FondoFilaAlterna;
        IContainer Celda(IContainer contenedor) => contenedor.Background(fondo).Padding(4);

        tabla.Cell().Element(Celda).Text((indice + 1).ToString());
        tabla.Cell().Element(Celda).Text(fila.NroComprobante);
        tabla.Cell().Element(Celda).Text(FormatosDeTexto.FechaYHora(fila.FechaVenta));
        tabla.Cell().Element(Celda).Text(fila.NitCi);
        tabla.Cell().Element(Celda).Text(fila.RazonSocial);
        tabla.Cell().Element(Celda).Text(fila.Cajero);
        tabla.Cell().Element(Celda).Text(TextosDelListadoVentas.Estado(fila.Estado)).FontColor(ColorDelEstado(fila.Estado));
        tabla.Cell().Element(Celda).AlignRight().Text(FormatosDeTexto.Moneda(fila.MontoTotal));
    }

    private static string ColorDelEstado(EstadoDeVenta estado) =>
        estado == EstadoDeVenta.Anulada ? EstilosDeReporte.AmbarOscuro : EstilosDeReporte.AzulOscuro;

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
                        $"Generado el {FormatosDeTexto.FechaYHora(_reporte.GeneradoEn)} por {_reporte.GeneradoPor}")
                        .FontSize(8);
                    fila.ConstantItem(120).AlignRight().DefaultTextStyle(estilo => estilo.FontSize(8)).Text(ComponerNumeroDePagina);
                });
            });

    private void ComponerSumariados(TextDescriptor texto)
    {
        var totales = _reporte.Totales;
        texto.Span($"Ventas completadas: {FormatosDeTexto.Entero(totales.CantidadDeCompletadas)}   |   ");
        texto.Span($"Ventas anuladas: {FormatosDeTexto.Entero(totales.CantidadDeAnuladas)}   |   ");
        texto.Span($"Monto total de las completadas: Bs {FormatosDeTexto.Moneda(totales.MontoTotalDeCompletadas)}   |   ");
        texto.Span($"Promedio por venta completada: Bs {FormatosDeTexto.Moneda(totales.PromedioPorVenta)}");
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
            .Padding(4)
            .DefaultTextStyle(estilo => estilo.FontColor(EstilosDeReporte.Blanco).Bold().FontSize(9));
}
