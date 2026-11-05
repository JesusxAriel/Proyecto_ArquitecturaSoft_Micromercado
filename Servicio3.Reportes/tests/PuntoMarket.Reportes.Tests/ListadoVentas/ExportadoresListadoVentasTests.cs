using System.Text;
using ClosedXML.Excel;
using PuntoMarket.Reportes.Application.Common;
using PuntoMarket.Reportes.Domain.ListadoVentas;
using PuntoMarket.Reportes.Domain.ValueObjects;
using PuntoMarket.Reportes.Infrastructure.Exportacion.ListadoVentas;
using PuntoMarket.Reportes.Infrastructure.Exportacion.VentasPorCategoria;

namespace PuntoMarket.Reportes.Tests.ListadoVentas;

public class ExportadoresListadoVentasTests
{
    private const int FilaDelPrimerDato = 2;

    private static ReporteListadoVentas CrearReporte(params FilaDeVenta[] filas) =>
        ReporteListadoVentas.Crear(
            filas,
            new FiltrosDeListadoVentas(RangoDeFechas.SinLimites(), null, null, 100),
            new DateTime(2026, 11, 3, 9, 15, 0),
            "alejandra");

    private static FilaDeVenta Fila(int id, EstadoDeVenta estado, decimal monto) =>
        new(
            id,
            $"F-{id:0000}",
            new DateTime(2026, 1, 15, 10, 0, 0),
            "1234567",
            "Comercial Andina",
            "ventas01",
            estado,
            monto);

    private static ReporteListadoVentas ReporteConDatos() =>
        CrearReporte(
            Fila(1, EstadoDeVenta.Completada, 120m),
            Fila(2, EstadoDeVenta.Completada, 80m),
            Fila(3, EstadoDeVenta.Anulada, 50m));

    [Fact]
    public void El_pdf_con_datos_comienza_con_la_firma_de_pdf()
    {
        var exportador = new ListadoVentasPdf(new GeneradorDeGrafico());

        var archivo = exportador.Exportar(ReporteConDatos());

        Assert.Equal(FormatoDeExportacion.Pdf, exportador.Formato);
        Assert.Equal("application/pdf", archivo.TipoDeContenido);
        Assert.StartsWith("%PDF", Encoding.ASCII.GetString(archivo.Contenido, 0, 4));
        Assert.StartsWith("listado-de-ventas-", archivo.NombreDeArchivo);
    }

    [Fact]
    public void El_pdf_sin_datos_se_genera_sin_romperse()
    {
        var archivo = new ListadoVentasPdf(new GeneradorDeGrafico()).Exportar(CrearReporte());

        Assert.StartsWith("%PDF", Encoding.ASCII.GetString(archivo.Contenido, 0, 4));
    }

    [Fact]
    public void El_excel_escribe_las_filas_el_estado_y_los_totales_en_la_hoja_listado()
    {
        var exportador = new ListadoVentasExcel(new GeneradorDeGrafico());

        var archivo = exportador.Exportar(ReporteConDatos());

        Assert.Equal(FormatoDeExportacion.Xlsx, exportador.Formato);
        Assert.Equal("PK", Encoding.ASCII.GetString(archivo.Contenido, 0, 2));

        using var libro = new XLWorkbook(new MemoryStream(archivo.Contenido));
        Assert.Equal(["Resumen", "Listado"], libro.Worksheets.Select(h => h.Name));
        var hoja = libro.Worksheet("Listado");
        Assert.Equal("Comprobante", hoja.Cell(1, 2).GetString());
        Assert.Equal("F-0001", hoja.Cell(FilaDelPrimerDato, 2).GetString());
        Assert.Equal("Completada", hoja.Cell(FilaDelPrimerDato, 7).GetString());
        Assert.Equal("Anulada", hoja.Cell(FilaDelPrimerDato + 2, 7).GetString());
        Assert.Equal(120d, hoja.Cell(FilaDelPrimerDato, 8).GetDouble());
        Assert.Equal("SUMIF(G2:G4,\"Completada\",H2:H4)", hoja.Cell(6, 8).FormulaA1);
        Assert.Contains("Monto total", hoja.Cell(6, 1).GetString());
        Assert.Equal(200d, hoja.Cell(6, 8).Value.GetNumber());
        Assert.True(hoja.AutoFilter.IsEnabled);
        Assert.Equal(1, hoja.SheetView.SplitRow);
    }

    [Fact]
    public void El_excel_escribe_los_sumariados_con_formulas_que_calculan()
    {
        var archivo = new ListadoVentasExcel(new GeneradorDeGrafico()).Exportar(ReporteConDatos());

        using var libro = new XLWorkbook(new MemoryStream(archivo.Contenido));
        var hoja = libro.Worksheet("Listado");
        Assert.Equal("COUNTIF(G2:G4,\"Completada\")", hoja.Cell(7, 8).FormulaA1);
        Assert.Equal("COUNTIF(G2:G4,\"Anulada\")", hoja.Cell(8, 8).FormulaA1);
        Assert.StartsWith("IFERROR(", hoja.Cell(9, 8).FormulaA1);
        Assert.Equal(2d, hoja.Cell(7, 8).Value.GetNumber());
        Assert.Equal(1d, hoja.Cell(8, 8).Value.GetNumber());
        Assert.Equal(100d, hoja.Cell(9, 8).Value.GetNumber());
    }

    [Fact]
    public void El_resumen_incluye_logo_indicadores_graficos_y_la_linea_de_reporte_generado()
    {
        var archivo = new ListadoVentasExcel(new GeneradorDeGrafico()).Exportar(ReporteConDatos());

        using var libro = new XLWorkbook(new MemoryStream(archivo.Contenido));
        var resumen = libro.Worksheet("Resumen");
        Assert.Equal(3, resumen.Pictures.Count);
        Assert.Equal(1, resumen.Pictures.Min(imagen => imagen.TopLeftCell.Address.RowNumber));
        Assert.Equal("Reporte generado: alejandra - 03/11/2026 09:15:00", resumen.Cell(6, 1).GetString());
        Assert.Equal("VENTAS COMPLETADAS", resumen.Cell(8, 1).GetString());
        Assert.Equal("Bs 200,00", resumen.Cell(9, 2).GetString());
        Assert.Equal("VENTAS ANULADAS", resumen.Cell(8, 5).GetString());
    }

    [Fact]
    public void El_excel_se_genera_con_una_sola_venta_y_con_muchas()
    {
        var una = new ListadoVentasExcel(new GeneradorDeGrafico()).Exportar(CrearReporte(Fila(1, EstadoDeVenta.Completada, 10m)));
        var muchas = new ListadoVentasExcel(new GeneradorDeGrafico()).Exportar(
            CrearReporte(Enumerable.Range(1, 300).Select(i => Fila(i, i % 7 == 0 ? EstadoDeVenta.Anulada : EstadoDeVenta.Completada, i)).ToArray()));

        using var libroUna = new XLWorkbook(new MemoryStream(una.Contenido));
        using var libroMuchas = new XLWorkbook(new MemoryStream(muchas.Contenido));
        Assert.Equal("F-0001", libroUna.Worksheet("Listado").Cell(2, 2).GetString());
        Assert.Equal("SUMIF(G2:G301,\"Completada\",H2:H301)", libroMuchas.Worksheet("Listado").Cell(303, 8).FormulaA1);
    }

    [Fact]
    public void El_excel_sin_datos_muestra_el_mensaje_en_el_resumen_y_no_crea_la_hoja_listado()
    {
        var archivo = new ListadoVentasExcel(new GeneradorDeGrafico()).Exportar(CrearReporte());

        using var libro = new XLWorkbook(new MemoryStream(archivo.Contenido));
        Assert.Equal(["Resumen"], libro.Worksheets.Select(h => h.Name));
        Assert.Equal("Sin resultados para los filtros seleccionados", libro.Worksheet("Resumen").Cell(8, 1).GetString());
        Assert.Single(libro.Worksheet("Resumen").Pictures);
    }

    [Fact]
    public void El_creador_devuelve_el_exportador_que_corresponde_al_formato()
    {
        var creador = new CreadorExportadorListadoVentas([new ListadoVentasPdf(new GeneradorDeGrafico()), new ListadoVentasExcel(new GeneradorDeGrafico())]);

        Assert.Equal("application/pdf", creador.Exportar(ReporteConDatos(), FormatoDeExportacion.Pdf).TipoDeContenido);
        Assert.EndsWith(".xlsx", creador.Exportar(ReporteConDatos(), FormatoDeExportacion.Xlsx).NombreDeArchivo);
    }

    [Fact]
    public void El_creador_rechaza_un_formato_no_soportado()
    {
        var creador = new CreadorExportadorListadoVentas([new ListadoVentasPdf(new GeneradorDeGrafico()), new ListadoVentasExcel(new GeneradorDeGrafico())]);

        Assert.Throws<FormatoDeExportacionNoSoportadoException>(
            () => creador.Exportar(ReporteConDatos(), FormatoDeExportacion.Json));
    }
}
