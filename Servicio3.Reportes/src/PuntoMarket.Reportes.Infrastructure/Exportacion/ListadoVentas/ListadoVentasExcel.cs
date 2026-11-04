using ClosedXML.Excel;
using PuntoMarket.Reportes.Application.Common;
using PuntoMarket.Reportes.Application.Ports.Exportacion;
using PuntoMarket.Reportes.Domain.ListadoVentas;
using PuntoMarket.Reportes.Infrastructure.Exportacion.Comunes;

namespace PuntoMarket.Reportes.Infrastructure.Exportacion.ListadoVentas;

public class ListadoVentasExcel : IExportadorDeReporte<ReporteListadoVentas>
{
    private const string TipoXlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const string NombreDeHoja = "Listado de ventas";
    private const string FormatoDeMoneda = "\"Bs\" #,##0.00";
    private const string FormatoDeEntero = "#,##0";
    private const string FormatoDeFecha = "dd/mm/yyyy hh:mm";
    private const int FilaDeEncabezado = 8;
    private const int Columnas = 8;
    private const int ColumnaDelEstado = 7;
    private const int ColumnaDelMonto = 8;

    public FormatoDeExportacion Formato => FormatoDeExportacion.Xlsx;

    public ArchivoGenerado Exportar(ReporteListadoVentas reporte)
    {
        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add(NombreDeHoja);

        EscribirTitulo(hoja, reporte);
        EscribirEncabezados(hoja);
        EscribirCuerpo(hoja, reporte);
        DefinirAnchos(hoja);
        hoja.SheetView.FreezeRows(FilaDeEncabezado);

        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        return new ArchivoGenerado(memoria.ToArray(), TipoXlsx, NombreDeArchivo(reporte));
    }

    private static void EscribirTitulo(IXLWorksheet hoja, ReporteListadoVentas reporte)
    {
        hoja.Cell(1, 1).Value = TextosDelListadoVentas.Titulo;
        hoja.Cell(1, 1).Style.Font.SetBold().Font.SetFontSize(18).Font.SetFontColor(XLColor.FromHtml(EstilosDeReporte.AzulOscuro));
        hoja.Cell(2, 1).Value = $"Período: {FormatosDeTexto.Periodo(reporte.Filtros.Rango)}";
        hoja.Cell(3, 1).Value = $"Estado: {TextosDelListadoVentas.EstadoFiltrado(reporte.Filtros)}";
        hoja.Cell(4, 1).Value = $"Cliente: {TextosDelListadoVentas.ClienteFiltrado(reporte.Filtros)}";
        hoja.Cell(5, 1).Value = $"Cantidad de ventas: {TextosDelListadoVentas.Limite(reporte.Filtros)}";
        hoja.Cell(6, 1).Value =
            $"Generado el {FormatosDeTexto.FechaYHora(reporte.GeneradoEn)} por {reporte.GeneradoPor}";
    }

    private static void EscribirEncabezados(IXLWorksheet hoja)
    {
        string[] encabezados =
            ["N.º", "Comprobante", "Fecha y hora", "NIT/CI", "Razón social", "Cajero", "Estado", "Monto (Bs)"];
        for (var columna = 0; columna < encabezados.Length; columna++)
            hoja.Cell(FilaDeEncabezado, columna + 1).Value = encabezados[columna];

        var rango = hoja.Range(FilaDeEncabezado, 1, FilaDeEncabezado, Columnas);
        rango.Style.Font.SetBold().Font.SetFontColor(XLColor.White);
        rango.Style.Fill.SetBackgroundColor(XLColor.FromHtml(EstilosDeReporte.AzulOscuro));
        rango.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
    }

    private static void EscribirCuerpo(IXLWorksheet hoja, ReporteListadoVentas reporte)
    {
        if (reporte.Filas.Count == 0)
        {
            EscribirSinResultados(hoja);
            return;
        }

        var primeraFila = FilaDeEncabezado + 1;
        var ultimaFila = primeraFila + reporte.Filas.Count - 1;
        var filaDeTotales = ultimaFila + 1;

        EscribirFilas(hoja, reporte, primeraFila);
        FormatearColumnas(hoja, primeraFila, ultimaFila);
        EscribirTotales(hoja, primeraFila, ultimaFila, filaDeTotales);
        EscribirSumariados(hoja, primeraFila, ultimaFila, filaDeTotales + 2);
    }

    private static void EscribirSinResultados(IXLWorksheet hoja)
    {
        var celda = hoja.Range(FilaDeEncabezado + 1, 1, FilaDeEncabezado + 1, Columnas).Merge();
        celda.Value = FormatosDeTexto.SinResultados;
        celda.Style.Font.SetItalic();
        celda.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
    }

    private static void EscribirFilas(IXLWorksheet hoja, ReporteListadoVentas reporte, int primeraFila)
    {
        for (var indice = 0; indice < reporte.Filas.Count; indice++)
        {
            var fila = primeraFila + indice;
            var datos = reporte.Filas[indice];
            hoja.Cell(fila, 1).Value = indice + 1;
            hoja.Cell(fila, 2).Value = datos.NroComprobante;
            hoja.Cell(fila, 3).Value = datos.FechaVenta;
            hoja.Cell(fila, 4).Value = datos.NitCi;
            hoja.Cell(fila, 5).Value = datos.RazonSocial;
            hoja.Cell(fila, 6).Value = datos.Cajero;
            hoja.Cell(fila, ColumnaDelEstado).Value = TextosDelListadoVentas.Estado(datos.Estado);
            hoja.Cell(fila, ColumnaDelMonto).Value = datos.MontoTotal;
        }
    }

    private static void FormatearColumnas(IXLWorksheet hoja, int primeraFila, int ultimaFila)
    {
        hoja.Range(primeraFila, 3, ultimaFila, 3).Style.NumberFormat.Format = FormatoDeFecha;
        hoja.Range(primeraFila, ColumnaDelMonto, ultimaFila, ColumnaDelMonto).Style.NumberFormat.Format = FormatoDeMoneda;
        hoja.Range(primeraFila, 1, ultimaFila, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
    }

    private static void EscribirTotales(IXLWorksheet hoja, int primeraFila, int ultimaFila, int filaDeTotales)
    {
        EscribirLinea(
            hoja,
            filaDeTotales,
            "Monto total de las completadas",
            SumaDeCompletadas(primeraFila, ultimaFila),
            FormatoDeMoneda);

        var rango = hoja.Range(filaDeTotales, 1, filaDeTotales, Columnas);
        rango.Style.Fill.SetBackgroundColor(XLColor.FromHtml(EstilosDeReporte.Ambar));
        rango.Style.Border.SetTopBorder(XLBorderStyleValues.Medium);
    }

    private static void EscribirSumariados(IXLWorksheet hoja, int primeraFila, int ultimaFila, int primeraFilaDelPie)
    {
        EscribirLinea(
            hoja,
            primeraFilaDelPie,
            "Ventas completadas",
            Conteo(EstadoDeVenta.Completada, primeraFila, ultimaFila),
            FormatoDeEntero);
        EscribirLinea(
            hoja,
            primeraFilaDelPie + 1,
            "Ventas anuladas",
            Conteo(EstadoDeVenta.Anulada, primeraFila, ultimaFila),
            FormatoDeEntero);
        EscribirLinea(
            hoja,
            primeraFilaDelPie + 2,
            "Promedio por venta completada",
            PromedioDeCompletadas(primeraFila, ultimaFila),
            FormatoDeMoneda);
    }

    private static void EscribirLinea(IXLWorksheet hoja, int fila, string etiqueta, string formula, string formato)
    {
        var celdaDeEtiqueta = hoja.Range(fila, 1, fila, Columnas - 1).Merge();
        celdaDeEtiqueta.Value = etiqueta;
        celdaDeEtiqueta.Style.Font.SetBold();
        celdaDeEtiqueta.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

        var celdaDelValor = hoja.Cell(fila, ColumnaDelMonto);
        celdaDelValor.FormulaA1 = formula;
        celdaDelValor.Style.Font.SetBold();
        celdaDelValor.Style.NumberFormat.Format = formato;
    }

    private static string Conteo(EstadoDeVenta estado, int primeraFila, int ultimaFila) =>
        $"COUNTIF({RangoDeEstados(primeraFila, ultimaFila)},\"{TextosDelListadoVentas.Estado(estado)}\")";

    private static string SumaDeCompletadas(int primeraFila, int ultimaFila) =>
        $"SUMIF({RangoDeEstados(primeraFila, ultimaFila)}," +
        $"\"{TextosDelListadoVentas.Estado(EstadoDeVenta.Completada)}\"," +
        $"{RangoDeMontos(primeraFila, ultimaFila)})";

    private static string PromedioDeCompletadas(int primeraFila, int ultimaFila) =>
        $"IFERROR({SumaDeCompletadas(primeraFila, ultimaFila)}/" +
        $"{Conteo(EstadoDeVenta.Completada, primeraFila, ultimaFila)},0)";

    private static string RangoDeEstados(int primeraFila, int ultimaFila) => $"G{primeraFila}:G{ultimaFila}";

    private static string RangoDeMontos(int primeraFila, int ultimaFila) => $"H{primeraFila}:H{ultimaFila}";

    private static void DefinirAnchos(IXLWorksheet hoja)
    {
        hoja.Column(1).Width = 6;
        hoja.Column(2).Width = 18;
        hoja.Column(3).Width = 18;
        hoja.Column(4).Width = 16;
        hoja.Column(5).Width = 34;
        hoja.Column(6).Width = 16;
        hoja.Column(ColumnaDelEstado).Width = 14;
        hoja.Column(ColumnaDelMonto).Width = 18;
    }

    private static string NombreDeArchivo(ReporteListadoVentas reporte) =>
        $"listado-de-ventas-{reporte.GeneradoEn:yyyyMMdd-HHmmss}.xlsx";
}
