using ClosedXML.Excel;
using PuntoMarket.Reportes.Application.Common;
using PuntoMarket.Reportes.Application.Ports.Exportacion;
using PuntoMarket.Reportes.Domain.Tendencias;
using PuntoMarket.Reportes.Domain.ValueObjects;
using PuntoMarket.Reportes.Domain.VentasPorCategoria;
using PuntoMarket.Reportes.Infrastructure.Exportacion.Comunes;

namespace PuntoMarket.Reportes.Infrastructure.Exportacion.VentasPorCategoria;

public class VentasPorCategoriaExcel : IExportadorDeReporte<ReporteVentasPorCategoria>
{
    private const string TipoXlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const string HojaResumen = "Resumen";
    private const string HojaProductos = "Productos";
    private const string HojaTendencia = "Tendencia";
    private const string FormatoDeMoneda = "\"Bs\" #,##0.00";
    private const string FormatoDeEntero = "#,##0";
    private const string FormatoDePorcentaje = "0.0%";
    private const string FormatoDeFecha = "dd/MM/yyyy";
    private const int ColumnaDelLogo = 4;
    private const int FilaDeTarjetas = 7;
    private const int FilaDeEncabezado = 11;

    private readonly GeneradorDeGrafico _generadorDeGrafico;

    public VentasPorCategoriaExcel(GeneradorDeGrafico generadorDeGrafico)
    {
        _generadorDeGrafico = generadorDeGrafico;
    }

    public FormatoDeExportacion Formato => FormatoDeExportacion.Xlsx;

    public ArchivoGenerado Exportar(ReporteVentasPorCategoria reporte)
    {
        using var libro = new XLWorkbook();
        var resumen = libro.Worksheets.Add(HojaResumen);
        var filaDeTotales = EscribirResumen(resumen, reporte);
        if (reporte.Filas.Count > 0)
            AgregarHojasDeDetalle(libro, reporte, filaDeTotales);

        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        return new ArchivoGenerado(memoria.ToArray(), TipoXlsx, NombreDeArchivo(reporte));
    }

    private int EscribirResumen(IXLWorksheet hoja, ReporteVentasPorCategoria reporte)
    {
        DefinirAnchos(hoja);
        ComponentesDeExcel.EscribirTitulo(hoja, TextosDelReporte.Titulo, LineasDeEncabezado(reporte), ColumnaDelLogo);
        if (reporte.Filas.Count == 0)
        {
            hoja.Cell(FilaDeTarjetas, 1).Value = FormatosDeTexto.SinResultados;
            hoja.Cell(FilaDeTarjetas, 1).Style.Font.SetItalic();
            return FilaDeTarjetas;
        }

        ComponentesDeExcel.EscribirTarjetas(hoja, FilaDeTarjetas, CrearTarjetas(reporte));
        var filaDeTotales = EscribirTabla(hoja, reporte);
        InsertarGraficos(hoja, reporte, filaDeTotales + 2);
        return filaDeTotales;
    }

    private static IReadOnlyList<string> LineasDeEncabezado(ReporteVentasPorCategoria reporte) =>
    [
        $"Período: {TextosDelReporte.Periodo(reporte.Filtros)}",
        $"Categoría: {TextosDelReporte.Categoria(reporte)}",
        $"Cantidad de categorías: {TextosDelReporte.Limite(reporte.Filtros)}",
        FormatosDeTexto.ReporteGenerado(reporte.GeneradoPor, reporte.GeneradoEn)
    ];

    private static IReadOnlyList<(Indicador Indicador, int ColumnaInicial, int ColumnaFinal)> CrearTarjetas(
        ReporteVentasPorCategoria reporte)
    {
        var totales = reporte.Totales;
        var lider = totales.CategoriaLider;
        var detalleDelLider = lider is null
            ? null
            : $"{FormatosDeTexto.Bolivianos(lider.MontoVendido)} ({FormatosDeTexto.Porcentaje(lider.MontoVendido, totales.TotalMonto)})";

        return
        [
            (new Indicador("MONTO TOTAL", FormatosDeTexto.Bolivianos(totales.TotalMonto)), 1, 2),
            (new Indicador("UNIDADES VENDIDAS", FormatosDeTexto.Entero(totales.TotalUnidades)), 3, 3),
            (new Indicador("CATEGORÍAS", FormatosDeTexto.Entero(reporte.Filas.Count)), 4, 4),
            (new Indicador("CATEGORÍA LÍDER", TextosDelReporte.Lider(totales), detalleDelLider), 5, 7)
        ];
    }

    private static int EscribirTabla(IXLWorksheet hoja, ReporteVentasPorCategoria reporte)
    {
        var primeraFila = FilaDeEncabezado + 1;
        var ultimaFila = FilaDeEncabezado + reporte.Filas.Count;
        var filaDeTotales = ultimaFila + 1;

        EscribirEncabezados(hoja);
        for (var indice = 0; indice < reporte.Filas.Count; indice++)
            EscribirFila(hoja, primeraFila + indice, indice, reporte.Filas[indice], filaDeTotales);

        ComponentesDeExcel.AlternarFilas(hoja, primeraFila, ultimaFila, 1, 5);
        EscribirTotales(hoja, primeraFila, ultimaFila, filaDeTotales);
        hoja.Range(FilaDeEncabezado, 1, ultimaFila, 5).SetAutoFilter();
        return filaDeTotales;
    }

    private static void EscribirEncabezados(IXLWorksheet hoja)
    {
        string[] encabezados = ["N.º", "Categoría", "Unidades vendidas", "Monto vendido (Bs)", "Participación"];
        for (var columna = 0; columna < encabezados.Length; columna++)
            hoja.Cell(FilaDeEncabezado, columna + 1).Value = encabezados[columna];

        ComponentesDeExcel.EstilizarEncabezadoDeTabla(hoja.Range(FilaDeEncabezado, 1, FilaDeEncabezado, encabezados.Length));
    }

    private static void EscribirFila(
        IXLWorksheet hoja,
        int fila,
        int indice,
        FilaDeCategoria categoria,
        int filaDeTotales)
    {
        hoja.Cell(fila, 1).Value = indice + 1;
        hoja.Cell(fila, 2).Value = categoria.NombreCategoria;
        hoja.Cell(fila, 3).Value = categoria.UnidadesVendidas;
        hoja.Cell(fila, 4).Value = categoria.MontoVendido;
        hoja.Cell(fila, 5).FormulaA1 = $"IFERROR(D{fila}/D${filaDeTotales},0)";
        hoja.Cell(fila, 3).Style.NumberFormat.Format = FormatoDeEntero;
        hoja.Cell(fila, 4).Style.NumberFormat.Format = FormatoDeMoneda;
        hoja.Cell(fila, 5).Style.NumberFormat.Format = FormatoDePorcentaje;
        hoja.Cell(fila, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
    }

    private static void EscribirTotales(IXLWorksheet hoja, int primeraFila, int ultimaFila, int filaDeTotales)
    {
        hoja.Cell(filaDeTotales, 2).Value = "Totales";
        hoja.Cell(filaDeTotales, 3).FormulaA1 = $"SUM(C{primeraFila}:C{ultimaFila})";
        hoja.Cell(filaDeTotales, 4).FormulaA1 = $"SUM(D{primeraFila}:D{ultimaFila})";
        hoja.Cell(filaDeTotales, 5).FormulaA1 = $"SUM(E{primeraFila}:E{ultimaFila})";
        hoja.Cell(filaDeTotales, 3).Style.NumberFormat.Format = FormatoDeEntero;
        hoja.Cell(filaDeTotales, 4).Style.NumberFormat.Format = FormatoDeMoneda;
        hoja.Cell(filaDeTotales, 5).Style.NumberFormat.Format = FormatoDePorcentaje;
        ComponentesDeExcel.EstilizarTotales(hoja.Range(filaDeTotales, 1, filaDeTotales, 5));
    }

    private void InsertarGraficos(IXLWorksheet hoja, ReporteVentasPorCategoria reporte, int filaInicial)
    {
        var fila = filaInicial;
        foreach (var grafico in GraficosDelResumen(reporte))
        {
            hoja.Cell(fila, 1).Value = grafico.Titulo;
            hoja.Cell(fila, 1).Style.Font.SetBold().Font.SetFontColor(ComponentesDeExcel.Color(EstilosDeReporte.AmbarOscuro));
            fila += 1 + ComponentesDeExcel.InsertarImagen(hoja, grafico, fila + 1, 1);
        }
    }

    private IEnumerable<ImagenDeGrafico> GraficosDelResumen(ReporteVentasPorCategoria reporte)
    {
        var graficos = GraficosDeVentasPorCategoria.Crear(_generadorDeGrafico, reporte);
        yield return graficos.MontoPorCategoria;
        yield return graficos.ParticipacionPorCategoria;
        if (graficos.Tendencia is { } tendencia)
            yield return tendencia;

        if (graficos.ProductosMasVendidos is { } productos)
            yield return productos;
    }

    private static void AgregarHojasDeDetalle(XLWorkbook libro, ReporteVentasPorCategoria reporte, int filaDeTotales)
    {
        if (reporte.ProductosMasVendidos is { Count: > 0 } productos)
            EscribirHojaDeProductos(libro.Worksheets.Add(HojaProductos), productos, filaDeTotales);

        if (reporte.Tendencia is { PuedeGraficarse: true } tendencia)
            EscribirHojaDeTendencia(libro.Worksheets.Add(HojaTendencia), tendencia);
    }

    private static void EscribirHojaDeProductos(
        IXLWorksheet hoja,
        IReadOnlyList<FilaDeProducto> productos,
        int filaDeTotalesDelResumen)
    {
        string[] encabezados = ["N.º", "Producto", "Unidades vendidas", "Monto vendido (Bs)", "Participación sobre el total"];
        for (var columna = 0; columna < encabezados.Length; columna++)
            hoja.Cell(1, columna + 1).Value = encabezados[columna];

        ComponentesDeExcel.EstilizarEncabezadoDeTabla(hoja.Range(1, 1, 1, encabezados.Length));
        for (var indice = 0; indice < productos.Count; indice++)
            EscribirFilaDeProducto(hoja, indice + 2, indice, productos[indice], filaDeTotalesDelResumen);

        ComponentesDeExcel.AlternarFilas(hoja, 2, productos.Count + 1, 1, encabezados.Length);
        hoja.Range(1, 1, productos.Count + 1, encabezados.Length).SetAutoFilter();
        hoja.SheetView.FreezeRows(1);
        hoja.Column(1).Width = 8;
        hoja.Column(2).Width = 46;
        hoja.Column(3).Width = 20;
        hoja.Column(4).Width = 22;
        hoja.Column(5).Width = 28;
    }

    private static void EscribirFilaDeProducto(
        IXLWorksheet hoja,
        int fila,
        int indice,
        FilaDeProducto producto,
        int filaDeTotalesDelResumen)
    {
        hoja.Cell(fila, 1).Value = indice + 1;
        hoja.Cell(fila, 2).Value = producto.NombreProducto;
        hoja.Cell(fila, 3).Value = producto.UnidadesVendidas;
        hoja.Cell(fila, 4).Value = producto.MontoVendido;
        hoja.Cell(fila, 5).FormulaA1 = $"IFERROR(D{fila}/{HojaResumen}!$D${filaDeTotalesDelResumen},0)";
        hoja.Cell(fila, 3).Style.NumberFormat.Format = FormatoDeEntero;
        hoja.Cell(fila, 4).Style.NumberFormat.Format = FormatoDeMoneda;
        hoja.Cell(fila, 5).Style.NumberFormat.Format = FormatoDePorcentaje;
        hoja.Cell(fila, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
    }

    private static void EscribirHojaDeTendencia(
        IXLWorksheet hoja,
        TendenciaDeVentas tendencia)
    {
        var diaria = tendencia.Agrupacion == AgrupacionDeTendencia.Diaria;
        hoja.Cell(1, 1).Value = diaria ? "Fecha" : "Semana (inicio en lunes)";
        hoja.Cell(1, 2).Value = "Monto vendido (Bs)";
        ComponentesDeExcel.EstilizarEncabezadoDeTabla(hoja.Range(1, 1, 1, 2));

        for (var indice = 0; indice < tendencia.Puntos.Count; indice++)
            EscribirPuntoDeTendencia(hoja, indice + 2, tendencia.Puntos[indice]);

        var ultimaFila = tendencia.Puntos.Count + 1;
        ComponentesDeExcel.AlternarFilas(hoja, 2, ultimaFila, 1, 2);
        EscribirTotalDeTendencia(hoja, ultimaFila);
        hoja.Range(1, 1, ultimaFila, 2).SetAutoFilter();
        hoja.SheetView.FreezeRows(1);
        hoja.Column(1).Width = 28;
        hoja.Column(2).Width = 24;
    }

    private static void EscribirPuntoDeTendencia(
        IXLWorksheet hoja,
        int fila,
        PuntoDeTendencia punto)
    {
        hoja.Cell(fila, 1).Value = punto.Fecha.ToDateTime(TimeOnly.MinValue);
        hoja.Cell(fila, 1).Style.DateFormat.Format = FormatoDeFecha;
        hoja.Cell(fila, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        hoja.Cell(fila, 2).Value = punto.Monto;
        hoja.Cell(fila, 2).Style.NumberFormat.Format = FormatoDeMoneda;
    }

    private static void EscribirTotalDeTendencia(IXLWorksheet hoja, int ultimaFila)
    {
        var fila = ultimaFila + 1;
        hoja.Cell(fila, 1).Value = "Total";
        hoja.Cell(fila, 2).FormulaA1 = $"SUM(B2:B{ultimaFila})";
        hoja.Cell(fila, 2).Style.NumberFormat.Format = FormatoDeMoneda;
        ComponentesDeExcel.EstilizarTotales(hoja.Range(fila, 1, fila, 2));
    }

    private static void DefinirAnchos(IXLWorksheet hoja)
    {
        hoja.Column(1).Width = 8;
        hoja.Column(2).Width = 38;
        hoja.Column(3).Width = 20;
        hoja.Column(4).Width = 22;
        hoja.Column(5).Width = 16;
        hoja.Column(6).Width = 16;
        hoja.Column(7).Width = 16;
    }

    private static string NombreDeArchivo(ReporteVentasPorCategoria reporte) =>
        $"ventas-por-categoria-{reporte.GeneradoEn:yyyyMMdd-HHmmss}.xlsx";
}
