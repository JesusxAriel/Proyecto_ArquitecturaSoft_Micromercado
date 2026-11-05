using ClosedXML.Excel;
using PuntoMarket.Reportes.Application.Common;
using PuntoMarket.Reportes.Application.Ports.Exportacion;
using PuntoMarket.Reportes.Domain.ListadoVentas;
using PuntoMarket.Reportes.Infrastructure.Exportacion.Comunes;
using PuntoMarket.Reportes.Infrastructure.Exportacion.VentasPorCategoria;

namespace PuntoMarket.Reportes.Infrastructure.Exportacion.ListadoVentas;

public class ListadoVentasExcel : IExportadorDeReporte<ReporteListadoVentas>
{
    private const string TipoXlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const string HojaResumen = "Resumen";
    private const string HojaListado = "Listado";
    private const string TextoCompletada = "Completada";
    private const string TextoAnulada = "Anulada";
    private const string FormatoDeMoneda = "\"Bs\" #,##0.00";
    private const string FormatoDeFechaYHora = "dd/MM/yyyy HH:mm";
    private const int ColumnaDelLogo = 4;
    private const int FilaDeTarjetas = 8;
    private const int Columnas = 8;
    private const int ColumnaDelEstado = 7;
    private const int ColumnaDelMonto = 8;

    private readonly GeneradorDeGrafico _generadorDeGrafico;

    public ListadoVentasExcel(GeneradorDeGrafico generadorDeGrafico)
    {
        _generadorDeGrafico = generadorDeGrafico;
    }

    public FormatoDeExportacion Formato => FormatoDeExportacion.Xlsx;

    public ArchivoGenerado Exportar(ReporteListadoVentas reporte)
    {
        using var libro = new XLWorkbook();
        EscribirResumen(libro.Worksheets.Add(HojaResumen), reporte);
        if (reporte.Filas.Count > 0)
            EscribirListado(libro.Worksheets.Add(HojaListado), reporte);

        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        return new ArchivoGenerado(memoria.ToArray(), TipoXlsx, NombreDeArchivo(reporte));
    }

    private void EscribirResumen(IXLWorksheet hoja, ReporteListadoVentas reporte)
    {
        DefinirAnchosDelResumen(hoja);
        ComponentesDeExcel.EscribirTitulo(hoja, TextosDelListadoVentas.Titulo, LineasDeEncabezado(reporte), ColumnaDelLogo);
        if (reporte.Filas.Count == 0)
        {
            hoja.Cell(FilaDeTarjetas, 1).Value = FormatosDeTexto.SinResultados;
            hoja.Cell(FilaDeTarjetas, 1).Style.Font.SetItalic();
            return;
        }

        var fila = ComponentesDeExcel.EscribirTarjetas(hoja, FilaDeTarjetas, CrearTarjetas(reporte.Totales));
        InsertarGraficos(hoja, reporte, fila + 1);
    }

    private static IReadOnlyList<string> LineasDeEncabezado(ReporteListadoVentas reporte) =>
    [
        $"Período: {FormatosDeTexto.Periodo(reporte.Filtros.Rango)}",
        $"Estado: {TextosDelListadoVentas.EstadoFiltrado(reporte.Filtros)}",
        $"Cliente: {TextosDelListadoVentas.ClienteFiltrado(reporte.Filtros)}",
        $"Cantidad de ventas: {TextosDelListadoVentas.Limite(reporte.Filtros)}",
        FormatosDeTexto.ReporteGenerado(reporte.GeneradoPor, reporte.GeneradoEn)
    ];

    private static IReadOnlyList<(Indicador Indicador, int ColumnaInicial, int ColumnaFinal)> CrearTarjetas(
        TotalesListadoVentas totales) =>
    [
        (new Indicador("VENTAS COMPLETADAS", FormatosDeTexto.Entero(totales.CantidadDeCompletadas)), 1, 1),
        (new Indicador("MONTO TOTAL", FormatosDeTexto.Bolivianos(totales.MontoTotalDeCompletadas)), 2, 2),
        (new Indicador("PROMEDIO POR VENTA", FormatosDeTexto.Bolivianos(totales.PromedioPorVenta)), 3, 3),
        (new Indicador("VENTA MÁS ALTA", FormatosDeTexto.Bolivianos(totales.VentaMasAlta)), 4, 4),
        (new Indicador("VENTAS ANULADAS", FormatosDeTexto.Entero(totales.CantidadDeAnuladas)), 5, 5)
    ];

    private void InsertarGraficos(IXLWorksheet hoja, ReporteListadoVentas reporte, int filaInicial)
    {
        var graficos = GraficosDeListadoVentas.Crear(_generadorDeGrafico, reporte);
        if (graficos is null)
            return;

        var fila = filaInicial;
        foreach (var grafico in new[] { graficos.MontoPorPeriodo, graficos.ParticipacionPorCliente })
        {
            hoja.Cell(fila, 1).Value = grafico.Titulo;
            hoja.Cell(fila, 1).Style.Font.SetBold().Font.SetFontColor(ComponentesDeExcel.Color(EstilosDeReporte.AmbarOscuro));
            fila += 1 + ComponentesDeExcel.InsertarImagen(hoja, grafico, fila + 1, 1);
        }
    }

    private static void EscribirListado(IXLWorksheet hoja, ReporteListadoVentas reporte)
    {
        var ultimaFila = reporte.Filas.Count + 1;

        EscribirEncabezados(hoja);
        for (var indice = 0; indice < reporte.Filas.Count; indice++)
            EscribirFila(hoja, indice + 2, indice, reporte.Filas[indice]);

        ComponentesDeExcel.AlternarFilas(hoja, 2, ultimaFila, 1, Columnas);
        AtenuarAnuladas(hoja, reporte, 2);
        EscribirTotales(hoja, ultimaFila + 2, 2, ultimaFila);
        hoja.Range(1, 1, ultimaFila, Columnas).SetAutoFilter();
        hoja.SheetView.FreezeRows(1);
        DefinirAnchosDelListado(hoja);
    }

    private static void EscribirEncabezados(IXLWorksheet hoja)
    {
        string[] encabezados = ["N.º", "Comprobante", "Fecha y hora", "NIT/CI", "Cliente", "Usuario", "Estado", "Monto (Bs)"];
        for (var columna = 0; columna < encabezados.Length; columna++)
            hoja.Cell(1, columna + 1).Value = encabezados[columna];

        ComponentesDeExcel.EstilizarEncabezadoDeTabla(hoja.Range(1, 1, 1, Columnas));
    }

    private static void EscribirFila(IXLWorksheet hoja, int fila, int indice, FilaDeVenta venta)
    {
        hoja.Cell(fila, 1).Value = indice + 1;
        hoja.Cell(fila, 2).Value = venta.NroComprobante;
        hoja.Cell(fila, 3).Value = venta.FechaVenta;
        hoja.Cell(fila, 3).Style.DateFormat.Format = FormatoDeFechaYHora;
        hoja.Cell(fila, 4).Value = venta.NitCi;
        hoja.Cell(fila, 5).Value = venta.RazonSocial;
        hoja.Cell(fila, 6).Value = venta.Cajero;
        hoja.Cell(fila, ColumnaDelEstado).Value = TextosDelListadoVentas.Estado(venta.Estado);
        hoja.Cell(fila, ColumnaDelMonto).Value = venta.MontoTotal;
        hoja.Cell(fila, ColumnaDelMonto).Style.NumberFormat.Format = FormatoDeMoneda;
        hoja.Cell(fila, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
    }

    private static void AtenuarAnuladas(IXLWorksheet hoja, ReporteListadoVentas reporte, int primeraFila)
    {
        for (var indice = 0; indice < reporte.Filas.Count; indice++)
            if (reporte.Filas[indice].Estado == EstadoDeVenta.Anulada)
                AtenuarFila(hoja.Range(primeraFila + indice, 1, primeraFila + indice, Columnas));
    }

    private static void AtenuarFila(IXLRange fila)
    {
        fila.Style.Font.SetItalic();
        fila.Style.Font.SetStrikethrough();
        fila.Style.Font.SetFontColor(ComponentesDeExcel.Color(EstilosDeReporte.TextoAtenuado));
    }

    private static void EscribirTotales(IXLWorksheet hoja, int fila, int primeraFila, int ultimaFila)
    {
        var estados = $"{Columna(ColumnaDelEstado)}{primeraFila}:{Columna(ColumnaDelEstado)}{ultimaFila}";
        var montos = $"{Columna(ColumnaDelMonto)}{primeraFila}:{Columna(ColumnaDelMonto)}{ultimaFila}";

        EscribirTotal(hoja, fila, "Monto total de las completadas", $"SUMIF({estados},\"{TextoCompletada}\",{montos})", FormatoDeMoneda);
        EscribirTotal(hoja, fila + 1, "Ventas completadas", $"COUNTIF({estados},\"{TextoCompletada}\")", "#,##0");
        EscribirTotal(hoja, fila + 2, "Ventas anuladas", $"COUNTIF({estados},\"{TextoAnulada}\")", "#,##0");
        EscribirTotal(
            hoja,
            fila + 3,
            "Promedio por venta completada",
            $"IFERROR(SUMIF({estados},\"{TextoCompletada}\",{montos})/COUNTIF({estados},\"{TextoCompletada}\"),0)",
            FormatoDeMoneda);
    }

    private static void EscribirTotal(IXLWorksheet hoja, int fila, string etiqueta, string formula, string formato)
    {
        hoja.Cell(fila, 1).Value = etiqueta;
        hoja.Cell(fila, ColumnaDelMonto).FormulaA1 = formula;
        hoja.Cell(fila, ColumnaDelMonto).Style.NumberFormat.Format = formato;
        ComponentesDeExcel.EstilizarTotales(hoja.Range(fila, 1, fila, Columnas));
    }

    private static string Columna(int indice) => ((char)('A' + indice - 1)).ToString();

    private static void DefinirAnchosDelResumen(IXLWorksheet hoja)
    {
        for (var columna = 1; columna <= 6; columna++)
            hoja.Column(columna).Width = 24;
    }

    private static void DefinirAnchosDelListado(IXLWorksheet hoja)
    {
        hoja.Column(1).Width = 8;
        hoja.Column(2).Width = 18;
        hoja.Column(3).Width = 20;
        hoja.Column(4).Width = 16;
        hoja.Column(5).Width = 38;
        hoja.Column(6).Width = 16;
        hoja.Column(ColumnaDelEstado).Width = 14;
        hoja.Column(ColumnaDelMonto).Width = 18;
    }

    private static string NombreDeArchivo(ReporteListadoVentas reporte) =>
        $"listado-de-ventas-{reporte.GeneradoEn:yyyyMMdd-HHmmss}.xlsx";
}
