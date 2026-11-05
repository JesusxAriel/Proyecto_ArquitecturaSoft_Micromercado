using PuntoMarket.Reportes.Domain.VentasPorCategoria;
using PuntoMarket.Reportes.Infrastructure.Exportacion.Comunes;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PuntoMarket.Reportes.Infrastructure.Exportacion.VentasPorCategoria;

public class VentasPorCategoriaDocumento : IDocument
{
    private const int MargenDePagina = 30;
    private const int EspacioEntreBloques = 10;

    private readonly ReporteVentasPorCategoria _reporte;
    private readonly GraficosDeVentasPorCategoria? _graficos;
    private readonly byte[] _logo;

    public VentasPorCategoriaDocumento(ReporteVentasPorCategoria reporte, GraficosDeVentasPorCategoria? graficos, byte[] logo)
    {
        _reporte = reporte;
        _graficos = graficos;
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
            pagina.Content().PaddingVertical(10).Element(ComponerContenido);
            pagina.Footer().Element(ComponerPie);
        });

    private void ComponerEncabezado(IContainer contenedor) =>
        ComponentesDePdf.Encabezado(
            contenedor,
            _logo,
            TextosDelReporte.Titulo,
            $"Período: {TextosDelReporte.Periodo(_reporte.Filtros)}");

    private void ComponerContenido(IContainer contenedor) =>
        contenedor.Column(columna =>
        {
            columna.Spacing(EspacioEntreBloques);
            columna.Item().Element(c => ComponentesDePdf.Filtros(c, LineasDeFiltros()));
            if (_reporte.Filas.Count == 0 || _graficos is null)
            {
                columna.Item().Element(ComponerSinResultados);
                return;
            }

            columna.Item().Element(c => ComponentesDePdf.Tarjetas(c, CrearIndicadores()));
            columna.Item().Element(ComponerGraficosPrincipales);
            columna.Item().Element(ComponerGraficosDeDetalle);
            columna.Item().Element(ComponerTabla);
        });

    private IEnumerable<string> LineasDeFiltros() =>
    [
        $"Período: {TextosDelReporte.Periodo(_reporte.Filtros)}",
        $"Categoría: {TextosDelReporte.Categoria(_reporte)}",
        $"Cantidad de categorías: {TextosDelReporte.Limite(_reporte.Filtros)}"
    ];

    private IReadOnlyList<Indicador> CrearIndicadores()
    {
        var totales = _reporte.Totales;
        var lider = totales.CategoriaLider;

        return
        [
            new Indicador("MONTO TOTAL", FormatosDeTexto.Bolivianos(totales.TotalMonto)),
            new Indicador("UNIDADES VENDIDAS", FormatosDeTexto.Entero(totales.TotalUnidades)),
            new Indicador("CATEGORÍAS", FormatosDeTexto.Entero(_reporte.Filas.Count)),
            new Indicador(
                "CATEGORÍA LÍDER",
                FormatosDeTexto.Abreviar(TextosDelReporte.Lider(totales), 20),
                lider is null
                    ? null
                    : $"{FormatosDeTexto.Bolivianos(lider.MontoVendido)} ({FormatosDeTexto.Porcentaje(lider.MontoVendido, totales.TotalMonto)})")
        ];
    }

    private void ComponerGraficosPrincipales(IContainer contenedor) =>
        contenedor.Row(fila =>
        {
            fila.Spacing(EspacioEntreBloques);
            fila.RelativeItem().Element(c => ComponentesDePdf.TarjetaDeGrafico(c, _graficos!.MontoPorCategoria));
            fila.RelativeItem().Element(c => ComponentesDePdf.TarjetaDeGrafico(c, _graficos!.ParticipacionPorCategoria));
        });

    private void ComponerGraficosDeDetalle(IContainer contenedor) =>
        contenedor.Column(columna =>
        {
            columna.Spacing(EspacioEntreBloques);
            if (_graficos!.Tendencia is { } tendencia)
                columna.Item().Element(c => ComponentesDePdf.TarjetaDeGrafico(c, tendencia));

            if (_graficos.ProductosMasVendidos is { } productos)
                columna.Item().Element(c => ComponentesDePdf.TarjetaDeGrafico(c, productos));
        });

    private static void ComponerSinResultados(IContainer contenedor) =>
        contenedor.PaddingVertical(30).AlignCenter().Text(FormatosDeTexto.SinResultados).FontSize(13).Italic();

    private void ComponerTabla(IContainer contenedor) =>
        contenedor.Column(columna =>
        {
            columna.Item().PaddingBottom(4).Text("Detalle por categoría").Bold().FontSize(11).FontColor(EstilosDeReporte.AmbarOscuro);
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
            columnas.ConstantColumn(32);
            columnas.RelativeColumn(3);
            columnas.RelativeColumn(1.4f);
            columnas.RelativeColumn(1.6f);
            columnas.RelativeColumn(1.3f);
        });

    private static void ComponerEncabezadoDeTabla(TableCellDescriptor encabezado)
    {
        encabezado.Cell().Element(ComponentesDePdf.CeldaDeEncabezado).Text("N.º");
        encabezado.Cell().Element(ComponentesDePdf.CeldaDeEncabezado).Text("Categoría");
        encabezado.Cell().Element(ComponentesDePdf.CeldaDeEncabezado).AlignRight().Text("Unidades vendidas");
        encabezado.Cell().Element(ComponentesDePdf.CeldaDeEncabezado).AlignRight().Text("Monto vendido (Bs)");
        encabezado.Cell().Element(ComponentesDePdf.CeldaDeEncabezado).AlignRight().Text("Participación");
    }

    private void ComponerFila(TableDescriptor tabla, FilaDeCategoria fila, int indice)
    {
        IContainer Celda(IContainer c) => ComponentesDePdf.CeldaDeFila(c, indice);

        tabla.Cell().Element(Celda).Text((indice + 1).ToString());
        tabla.Cell().Element(Celda).Text(fila.NombreCategoria);
        tabla.Cell().Element(Celda).AlignRight().Text(FormatosDeTexto.Entero(fila.UnidadesVendidas));
        tabla.Cell().Element(Celda).AlignRight().Text(FormatosDeTexto.Moneda(fila.MontoVendido));
        tabla.Cell().Element(Celda).AlignRight()
            .Text(FormatosDeTexto.Porcentaje(fila.MontoVendido, _reporte.Totales.TotalMonto));
    }

    private void ComponerTotales(TableDescriptor tabla)
    {
        var totales = _reporte.Totales;

        tabla.Cell().Element(ComponentesDePdf.CeldaDeTotales).Text(string.Empty);
        tabla.Cell().Element(ComponentesDePdf.CeldaDeTotales).Text("Totales");
        tabla.Cell().Element(ComponentesDePdf.CeldaDeTotales).AlignRight().Text(FormatosDeTexto.Entero(totales.TotalUnidades));
        tabla.Cell().Element(ComponentesDePdf.CeldaDeTotales).AlignRight().Text(FormatosDeTexto.Moneda(totales.TotalMonto));
        tabla.Cell().Element(ComponentesDePdf.CeldaDeTotales).AlignRight()
            .Text(FormatosDeTexto.Porcentaje(totales.TotalMonto, totales.TotalMonto));
    }

    private void ComponerPie(IContainer contenedor) =>
        ComponentesDePdf.Pie(
            contenedor,
            $"Total de unidades: {FormatosDeTexto.Entero(_reporte.Totales.TotalUnidades)}   |   " +
            $"Monto total: {FormatosDeTexto.Bolivianos(_reporte.Totales.TotalMonto)}   |   " +
            $"Categoría líder: {TextosDelReporte.Lider(_reporte.Totales)}",
            FormatosDeTexto.ReporteGenerado(_reporte.GeneradoPor, _reporte.GeneradoEn));
}
