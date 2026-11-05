using PuntoMarket.Reportes.Domain.ListadoVentas;
using PuntoMarket.Reportes.Infrastructure.Exportacion.Comunes;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PuntoMarket.Reportes.Infrastructure.Exportacion.ListadoVentas;

public class ListadoVentasDocumento : IDocument
{
    private const int MargenDePagina = 28;
    private const int EspacioEntreBloques = 8;
    private const int ColumnasDeLaTabla = 8;

    private readonly ReporteListadoVentas _reporte;
    private readonly GraficosDeListadoVentas? _graficos;
    private readonly byte[] _logo;

    public ListadoVentasDocumento(ReporteListadoVentas reporte, GraficosDeListadoVentas? graficos, byte[] logo)
    {
        _reporte = reporte;
        _graficos = graficos;
        _logo = logo;
    }

    public DocumentMetadata GetMetadata() => new() { Title = TextosDelListadoVentas.Titulo };

    public void Compose(IDocumentContainer documento) =>
        documento.Page(pagina =>
        {
            pagina.Size(PageSizes.A4.Landscape());
            pagina.Margin(MargenDePagina);
            pagina.DefaultTextStyle(estilo => estilo.FontSize(10).FontColor(EstilosDeReporte.AzulOscuro));
            pagina.Header().Element(ComponerEncabezado);
            pagina.Content().PaddingVertical(8).Element(ComponerContenido);
            pagina.Footer().Element(ComponerPie);
        });

    private void ComponerEncabezado(IContainer contenedor) =>
        ComponentesDePdf.Encabezado(
            contenedor,
            _logo,
            TextosDelListadoVentas.Titulo,
            $"Período: {FormatosDeTexto.Periodo(_reporte.Filtros.Rango)}");

    private void ComponerContenido(IContainer contenedor) =>
        contenedor.Column(columna =>
        {
            columna.Spacing(EspacioEntreBloques);
            columna.Item().Element(c => ComponentesDePdf.FiltrosCompactos(c, LineasDeFiltros()));
            if (_reporte.Filas.Count == 0)
            {
                columna.Item().Element(ComponerSinResultados);
                return;
            }

            columna.Item().Element(c => ComponentesDePdf.Tarjetas(c, CrearIndicadores()));
            if (_graficos is not null)
            {
                columna.Item().Element(ComponerGraficos);
                columna.Item().PageBreak();
            }

            columna.Item().Element(ComponerTabla);
        });

    private IEnumerable<string> LineasDeFiltros() =>
    [
        $"Período: {FormatosDeTexto.Periodo(_reporte.Filtros.Rango)}",
        $"Estado: {TextosDelListadoVentas.EstadoFiltrado(_reporte.Filtros)}",
        $"Cliente: {TextosDelListadoVentas.ClienteFiltrado(_reporte.Filtros)}",
        $"Cantidad de ventas: {TextosDelListadoVentas.Limite(_reporte.Filtros)}"
    ];

    private IReadOnlyList<Indicador> CrearIndicadores()
    {
        var totales = _reporte.Totales;

        return
        [
            new Indicador("VENTAS COMPLETADAS", FormatosDeTexto.Entero(totales.CantidadDeCompletadas)),
            new Indicador("MONTO TOTAL", FormatosDeTexto.Bolivianos(totales.MontoTotalDeCompletadas)),
            new Indicador("PROMEDIO POR VENTA", FormatosDeTexto.Bolivianos(totales.PromedioPorVenta)),
            new Indicador("VENTA MÁS ALTA", FormatosDeTexto.Bolivianos(totales.VentaMasAlta)),
            new Indicador("VENTAS ANULADAS", FormatosDeTexto.Entero(totales.CantidadDeAnuladas))
        ];
    }

    private void ComponerGraficos(IContainer contenedor) =>
        contenedor.Row(fila =>
        {
            fila.Spacing(EspacioEntreBloques);
            fila.RelativeItem().Element(c => ComponentesDePdf.TarjetaDeGrafico(c, _graficos!.MontoPorDia));
            fila.RelativeItem().Element(c => ComponentesDePdf.TarjetaDeGrafico(c, _graficos!.ParticipacionPorCliente));
        });

    private static void ComponerSinResultados(IContainer contenedor) =>
        contenedor.PaddingVertical(30).AlignCenter().Text(FormatosDeTexto.SinResultados).FontSize(13).Italic();

    private void ComponerTabla(IContainer contenedor) =>
        contenedor.Column(columna =>
        {
            columna.Item().PaddingBottom(4).Text("Detalle de ventas").Bold().FontSize(11).FontColor(EstilosDeReporte.AmbarOscuro);
            columna.Item().Table(tabla =>
            {
                DefinirColumnas(tabla);
                tabla.Header(ComponerEncabezadoDeTabla);
                for (var indice = 0; indice < _reporte.Filas.Count; indice++)
                    ComponerFila(tabla, _reporte.Filas[indice], indice);

                ComponerTotales(tabla);
            });
        });

    private static void DefinirColumnas(TableDescriptor tabla) =>
        tabla.ColumnsDefinition(columnas =>
        {
            columnas.ConstantColumn(30);
            columnas.RelativeColumn(1.6f);
            columnas.RelativeColumn(1.7f);
            columnas.RelativeColumn(1.4f);
            columnas.RelativeColumn(3);
            columnas.RelativeColumn(1.4f);
            columnas.RelativeColumn(1.2f);
            columnas.RelativeColumn(1.6f);
        });

    private static void ComponerEncabezadoDeTabla(TableCellDescriptor encabezado)
    {
        encabezado.Cell().Element(ComponentesDePdf.CeldaDeEncabezado).Text("N.º");
        encabezado.Cell().Element(ComponentesDePdf.CeldaDeEncabezado).Text("Comprobante");
        encabezado.Cell().Element(ComponentesDePdf.CeldaDeEncabezado).Text("Fecha y hora");
        encabezado.Cell().Element(ComponentesDePdf.CeldaDeEncabezado).Text("NIT/CI");
        encabezado.Cell().Element(ComponentesDePdf.CeldaDeEncabezado).Text("Cliente");
        encabezado.Cell().Element(ComponentesDePdf.CeldaDeEncabezado).Text("Usuario");
        encabezado.Cell().Element(ComponentesDePdf.CeldaDeEncabezado).Text("Estado");
        encabezado.Cell().Element(ComponentesDePdf.CeldaDeEncabezado).AlignRight().Text("Monto (Bs)");
    }

    private static void ComponerFila(TableDescriptor tabla, FilaDeVenta fila, int indice)
    {
        var anulada = fila.Estado == EstadoDeVenta.Anulada;
        IContainer Celda(IContainer c) => ComponentesDePdf.CeldaDeFila(c, indice);

        Texto(tabla.Cell().Element(Celda), (indice + 1).ToString(), anulada);
        Texto(tabla.Cell().Element(Celda), fila.NroComprobante, anulada);
        Texto(tabla.Cell().Element(Celda), FormatosDeTexto.FechaYHora(fila.FechaVenta), anulada);
        Texto(tabla.Cell().Element(Celda), fila.NitCi, anulada);
        Texto(tabla.Cell().Element(Celda), fila.RazonSocial, anulada);
        Texto(tabla.Cell().Element(Celda), fila.Cajero, anulada);
        ComponerEstado(tabla.Cell().Element(Celda), fila.Estado);
        Texto(tabla.Cell().Element(Celda).AlignRight(), FormatosDeTexto.Moneda(fila.MontoTotal), anulada);
    }

    private static void Texto(IContainer contenedor, string texto, bool anulada)
    {
        var descriptor = contenedor.Text(texto);
        if (anulada)
            descriptor.FontColor(EstilosDeReporte.TextoAtenuado).Italic().Strikethrough();
    }

    private static void ComponerEstado(IContainer contenedor, EstadoDeVenta estado)
    {
        var descriptor = contenedor.Text(TextosDelListadoVentas.Estado(estado));
        if (estado == EstadoDeVenta.Anulada)
            descriptor.FontColor(EstilosDeReporte.AmbarOscuro).Bold();
    }

    private void ComponerTotales(TableDescriptor tabla)
    {
        var totales = _reporte.Totales;

        for (var columna = 0; columna < ColumnasDeLaTabla - 1; columna++)
            tabla.Cell().Element(ComponentesDePdf.CeldaDeTotales).Text(TextoDeTotales(columna, totales));

        tabla.Cell().Element(ComponentesDePdf.CeldaDeTotales).AlignRight()
            .Text(FormatosDeTexto.Moneda(totales.MontoTotalDeCompletadas));
    }

    private static string TextoDeTotales(int columna, TotalesListadoVentas totales) =>
        columna switch
        {
            1 => "Totales",
            4 => $"{FormatosDeTexto.Entero(totales.CantidadDeCompletadas)} ventas completadas",
            _ => string.Empty
        };

    private void ComponerPie(IContainer contenedor)
    {
        var totales = _reporte.Totales;
        ComponentesDePdf.Pie(
            contenedor,
            $"Ventas completadas: {FormatosDeTexto.Entero(totales.CantidadDeCompletadas)}   |   " +
            $"Ventas anuladas: {FormatosDeTexto.Entero(totales.CantidadDeAnuladas)}   |   " +
            $"Monto total de las completadas: {FormatosDeTexto.Bolivianos(totales.MontoTotalDeCompletadas)}   |   " +
            $"Promedio por venta completada: {FormatosDeTexto.Bolivianos(totales.PromedioPorVenta)}",
            FormatosDeTexto.ReporteGenerado(_reporte.GeneradoPor, _reporte.GeneradoEn));
    }
}
