using ClosedXML.Excel;
using PuntoMarket.Reportes.Application.Common;
using PuntoMarket.Reportes.Application.Ports.Exportacion;
using PuntoMarket.Reportes.Domain.VentasPorCategoria;
using PuntoMarket.Reportes.Infrastructure.Exportacion.Comunes;

namespace PuntoMarket.Reportes.Infrastructure.Exportacion.VentasPorCategoria;

public class VentasPorCategoriaExcel : IExportadorDeReporte<ReporteVentasPorCategoria>
{
    private const string TipoXlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const string NombreDeHoja = "Ventas por categoría";
    private const string FormatoDeMoneda = "\"Bs\" #,##0.00";
    private const string FormatoDeEntero = "#,##0";
    private const string FormatoDePorcentaje = "0.0%";
    private const int FilaDeEncabezado = 7;
    private const int ColumnaDelGrafico = 7;

    private readonly GeneradorDeGrafico _generadorDeGrafico;

    public VentasPorCategoriaExcel(GeneradorDeGrafico generadorDeGrafico)
    {
        _generadorDeGrafico = generadorDeGrafico;
    }

    public FormatoDeExportacion Formato => FormatoDeExportacion.Xlsx;

    public ArchivoGenerado Exportar(ReporteVentasPorCategoria reporte)
    {
        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add(NombreDeHoja);

        EscribirTitulo(hoja, reporte);
        EscribirEncabezados(hoja);
        EscribirCuerpo(hoja, reporte);
        DefinirAnchos(hoja);

        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        return new ArchivoGenerado(memoria.ToArray(), TipoXlsx, NombreDeArchivo(reporte));
    }

    private static void EscribirTitulo(IXLWorksheet hoja, ReporteVentasPorCategoria reporte)
    {
        hoja.Cell(1, 1).Value = TextosDelReporte.Titulo;
        hoja.Cell(1, 1).Style.Font.SetBold().Font.SetFontSize(18).Font.SetFontColor(XLColor.FromHtml(EstilosDeReporte.AzulOscuro));
        hoja.Cell(2, 1).Value = $"Período: {TextosDelReporte.Periodo(reporte.Filtros)}";
        hoja.Cell(3, 1).Value = $"Categoría: {TextosDelReporte.Categoria(reporte)}";
        hoja.Cell(4, 1).Value = $"Cantidad de categorías: {TextosDelReporte.Limite(reporte.Filtros)}";
        hoja.Cell(5, 1).Value =
            $"Generado el {TextosDelReporte.FechaYHora(reporte.GeneradoEn)} por {reporte.GeneradoPor}";
    }

    private static void EscribirEncabezados(IXLWorksheet hoja)
    {
        string[] encabezados = ["N.º", "Categoría", "Unidades vendidas", "Monto vendido (Bs)", "Participación"];
        for (var columna = 0; columna < encabezados.Length; columna++)
            hoja.Cell(FilaDeEncabezado, columna + 1).Value = encabezados[columna];

        var rango = hoja.Range(FilaDeEncabezado, 1, FilaDeEncabezado, encabezados.Length);
        rango.Style.Font.SetBold().Font.SetFontColor(XLColor.White);
        rango.Style.Fill.SetBackgroundColor(XLColor.FromHtml(EstilosDeReporte.AzulOscuro));
        rango.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
    }

    private void EscribirCuerpo(IXLWorksheet hoja, ReporteVentasPorCategoria reporte)
    {
        if (reporte.Filas.Count == 0)
        {
            EscribirSinResultados(hoja);
            return;
        }

        var primeraFila = FilaDeEncabezado + 1;
        var filaDeTotales = primeraFila + reporte.Filas.Count;

        EscribirFilas(hoja, reporte, primeraFila, filaDeTotales);
        EscribirTotales(hoja, primeraFila, filaDeTotales);
        InsertarGrafico(hoja, reporte);
    }

    private static void EscribirSinResultados(IXLWorksheet hoja)
    {
        var celda = hoja.Range(FilaDeEncabezado + 1, 1, FilaDeEncabezado + 1, 5).Merge();
        celda.Value = TextosDelReporte.SinResultados;
        celda.Style.Font.SetItalic();
        celda.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
    }

    private static void EscribirFilas(IXLWorksheet hoja, ReporteVentasPorCategoria reporte, int primeraFila, int filaDeTotales)
    {
        for (var indice = 0; indice < reporte.Filas.Count; indice++)
        {
            var fila = primeraFila + indice;
            var datos = reporte.Filas[indice];
            hoja.Cell(fila, 1).Value = indice + 1;
            hoja.Cell(fila, 2).Value = datos.NombreCategoria;
            hoja.Cell(fila, 3).Value = datos.UnidadesVendidas;
            hoja.Cell(fila, 4).Value = datos.MontoVendido;
            hoja.Cell(fila, 5).FormulaA1 = $"IFERROR(D{fila}/D${filaDeTotales},0)";
        }

        FormatearColumnas(hoja, primeraFila, filaDeTotales);
    }

    private static void FormatearColumnas(IXLWorksheet hoja, int primeraFila, int filaDeTotales)
    {
        hoja.Range(primeraFila, 3, filaDeTotales, 3).Style.NumberFormat.Format = FormatoDeEntero;
        hoja.Range(primeraFila, 4, filaDeTotales, 4).Style.NumberFormat.Format = FormatoDeMoneda;
        hoja.Range(primeraFila, 5, filaDeTotales, 5).Style.NumberFormat.Format = FormatoDePorcentaje;
    }

    private static void EscribirTotales(IXLWorksheet hoja, int primeraFila, int filaDeTotales)
    {
        var ultimaFila = filaDeTotales - 1;
        hoja.Cell(filaDeTotales, 2).Value = "Totales";
        hoja.Cell(filaDeTotales, 3).FormulaA1 = $"SUM(C{primeraFila}:C{ultimaFila})";
        hoja.Cell(filaDeTotales, 4).FormulaA1 = $"SUM(D{primeraFila}:D{ultimaFila})";
        hoja.Cell(filaDeTotales, 5).FormulaA1 = $"SUM(E{primeraFila}:E{ultimaFila})";

        var rango = hoja.Range(filaDeTotales, 1, filaDeTotales, 5);
        rango.Style.Font.SetBold();
        rango.Style.Fill.SetBackgroundColor(XLColor.FromHtml(EstilosDeReporte.Ambar));
        rango.Style.Border.SetTopBorder(XLBorderStyleValues.Medium);
    }

    private void InsertarGrafico(IXLWorksheet hoja, ReporteVentasPorCategoria reporte)
    {
        using var imagen = new MemoryStream(_generadorDeGrafico.GenerarBarras(reporte.Filas));
        hoja.AddPicture(imagen).MoveTo(hoja.Cell(FilaDeEncabezado, ColumnaDelGrafico));
    }

    private static void DefinirAnchos(IXLWorksheet hoja)
    {
        hoja.Column(1).Width = 8;
        hoja.Column(2).Width = 32;
        hoja.Column(3).Width = 20;
        hoja.Column(4).Width = 22;
        hoja.Column(5).Width = 16;
    }

    private static string NombreDeArchivo(ReporteVentasPorCategoria reporte) =>
        $"ventas-por-categoria-{reporte.GeneradoEn:yyyyMMdd-HHmmss}.xlsx";
}
