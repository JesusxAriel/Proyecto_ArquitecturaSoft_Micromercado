using ClosedXML.Excel;
using PuntoMarket.Reportes.Application.Common;
using PuntoMarket.Reportes.Domain.ValueObjects;
using PuntoMarket.Reportes.Domain.VentasPorCategoria;
using PuntoMarket.Reportes.Infrastructure.Exportacion.VentasPorCategoria;

namespace PuntoMarket.Reportes.Tests.VentasPorCategoria;

public class ExportadoresVentasPorCategoriaTests
{
    private static readonly GeneradorDeGrafico Grafico = new();

    private static ReporteVentasPorCategoria CrearReporte(params FilaDeCategoria[] filas) =>
        ReporteVentasPorCategoria.Crear(
            filas,
            new FiltrosDeVentasPorCategoria(RangoDeFechas.SinLimites(), null, null),
            new DateTime(2026, 5, 20, 14, 30, 0),
            "ariana");

    private static ReporteVentasPorCategoria ReporteConDatos() =>
        CrearReporte(
            new FilaDeCategoria(1, "Bebidas", 10, 100m),
            new FilaDeCategoria(2, "Lácteos", 4, 60.50m));

    [Fact]
    public void El_pdf_con_datos_comienza_con_la_firma_de_pdf()
    {
        var archivo = new VentasPorCategoriaPdf(Grafico).Exportar(ReporteConDatos());

        Assert.Equal(FormatoDeExportacion.Pdf, new VentasPorCategoriaPdf(Grafico).Formato);
        Assert.Equal("application/pdf", archivo.TipoDeContenido);
        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(archivo.Contenido, 0, 4));
    }

    [Fact]
    public void El_pdf_sin_datos_se_genera_sin_romperse()
    {
        var archivo = new VentasPorCategoriaPdf(Grafico).Exportar(CrearReporte());

        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(archivo.Contenido, 0, 4));
    }

    [Fact]
    public void El_excel_es_un_zip_con_las_filas_y_la_fila_de_totales()
    {
        var archivo = new VentasPorCategoriaExcel(Grafico).Exportar(ReporteConDatos());

        Assert.Equal("PK", System.Text.Encoding.ASCII.GetString(archivo.Contenido, 0, 2));
        using var libro = new XLWorkbook(new MemoryStream(archivo.Contenido));
        var hoja = libro.Worksheet(1);
        Assert.StartsWith("Reporte generado: ", hoja.Cell(5, 1).GetString());
        Assert.Matches(@" - \d{2}/\d{2}/\d{4} \d{2}:\d{2}:\d{2}$", hoja.Cell(5, 1).GetString());
        Assert.Equal("Bebidas", hoja.Cell(8, 2).GetString());
        Assert.Equal("Lácteos", hoja.Cell(9, 2).GetString());
        Assert.Equal("Totales", hoja.Cell(10, 2).GetString());
        Assert.Equal("SUM(D8:D9)", hoja.Cell(10, 4).FormulaA1);
        Assert.Single(hoja.Pictures);
    }

    [Fact]
    public void El_excel_sin_datos_muestra_la_fila_de_sin_resultados()
    {
        var archivo = new VentasPorCategoriaExcel(Grafico).Exportar(CrearReporte());

        using var libro = new XLWorkbook(new MemoryStream(archivo.Contenido));
        Assert.Equal("Sin resultados para los filtros seleccionados", libro.Worksheet(1).Cell(8, 1).GetString());
    }

    [Fact]
    public void El_creador_devuelve_el_exportador_segun_el_formato()
    {
        var creador = new CreadorExportadorVentasPorCategoria(
            [new VentasPorCategoriaPdf(Grafico), new VentasPorCategoriaExcel(Grafico)]);

        Assert.Equal("application/pdf", creador.Exportar(ReporteConDatos(), FormatoDeExportacion.Pdf).TipoDeContenido);
        Assert.EndsWith(".xlsx", creador.Exportar(ReporteConDatos(), FormatoDeExportacion.Xlsx).NombreDeArchivo);
        Assert.Throws<FormatoDeExportacionNoSoportadoException>(
            () => creador.Exportar(ReporteConDatos(), FormatoDeExportacion.Json));
    }
}
