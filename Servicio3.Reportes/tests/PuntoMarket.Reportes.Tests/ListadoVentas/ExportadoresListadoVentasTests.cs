using System.Text;
using ClosedXML.Excel;
using PuntoMarket.Reportes.Application.Common;
using PuntoMarket.Reportes.Domain.ListadoVentas;
using PuntoMarket.Reportes.Domain.ValueObjects;
using PuntoMarket.Reportes.Infrastructure.Exportacion.ListadoVentas;

namespace PuntoMarket.Reportes.Tests.ListadoVentas;

public class ExportadoresListadoVentasTests
{
    private const int FilaDelPrimerDato = 9;

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
        var exportador = new ListadoVentasPdf();

        var archivo = exportador.Exportar(ReporteConDatos());

        Assert.Equal(FormatoDeExportacion.Pdf, exportador.Formato);
        Assert.Equal("application/pdf", archivo.TipoDeContenido);
        Assert.StartsWith("%PDF", Encoding.ASCII.GetString(archivo.Contenido, 0, 4));
        Assert.StartsWith("listado-de-ventas-", archivo.NombreDeArchivo);
    }

    [Fact]
    public void El_pdf_sin_datos_se_genera_sin_romperse()
    {
        var archivo = new ListadoVentasPdf().Exportar(CrearReporte());

        Assert.StartsWith("%PDF", Encoding.ASCII.GetString(archivo.Contenido, 0, 4));
    }

    [Fact]
    public void El_excel_escribe_las_filas_el_estado_y_la_fila_de_totales()
    {
        var exportador = new ListadoVentasExcel();

        var archivo = exportador.Exportar(ReporteConDatos());

        Assert.Equal(FormatoDeExportacion.Xlsx, exportador.Formato);
        Assert.Equal("PK", Encoding.ASCII.GetString(archivo.Contenido, 0, 2));

        using var libro = new XLWorkbook(new MemoryStream(archivo.Contenido));
        var hoja = libro.Worksheet(1);
        Assert.Single(hoja.Pictures);
        Assert.Equal(1, hoja.Pictures.Single().TopLeftCell.Address.RowNumber);
        Assert.Equal("Comprobante", hoja.Cell(8, 2).GetString());
        Assert.Equal("Reporte generado: alejandra - 03/11/2026 09:15:00", hoja.Cell(6, 1).GetString());
        Assert.Equal("F-0001", hoja.Cell(FilaDelPrimerDato, 2).GetString());
        Assert.Equal("Completada", hoja.Cell(FilaDelPrimerDato, 7).GetString());
        Assert.Equal("Anulada", hoja.Cell(FilaDelPrimerDato + 2, 7).GetString());
        Assert.Equal(120d, hoja.Cell(FilaDelPrimerDato, 8).GetDouble());
        Assert.Equal("SUMIF(G9:G11,\"Completada\",H9:H11)", hoja.Cell(12, 8).FormulaA1);
        Assert.Contains("Monto total", hoja.Cell(12, 1).GetString());
    }

    [Fact]
    public void El_excel_escribe_los_sumariados_con_formulas()
    {
        var archivo = new ListadoVentasExcel().Exportar(ReporteConDatos());

        using var libro = new XLWorkbook(new MemoryStream(archivo.Contenido));
        var hoja = libro.Worksheet(1);
        Assert.Equal("COUNTIF(G9:G11,\"Completada\")", hoja.Cell(14, 8).FormulaA1);
        Assert.Equal("COUNTIF(G9:G11,\"Anulada\")", hoja.Cell(15, 8).FormulaA1);
        Assert.StartsWith("IFERROR(", hoja.Cell(16, 8).FormulaA1);
    }

    [Fact]
    public void El_excel_sin_datos_muestra_la_fila_de_sin_resultados()
    {
        var archivo = new ListadoVentasExcel().Exportar(CrearReporte());

        using var libro = new XLWorkbook(new MemoryStream(archivo.Contenido));
        Assert.Equal(
            "Sin resultados para los filtros seleccionados",
            libro.Worksheet(1).Cell(FilaDelPrimerDato, 1).GetString());
    }

    [Fact]
    public void El_creador_devuelve_el_exportador_que_corresponde_al_formato()
    {
        var creador = new CreadorExportadorListadoVentas([new ListadoVentasPdf(), new ListadoVentasExcel()]);

        Assert.Equal("application/pdf", creador.Exportar(ReporteConDatos(), FormatoDeExportacion.Pdf).TipoDeContenido);
        Assert.EndsWith(".xlsx", creador.Exportar(ReporteConDatos(), FormatoDeExportacion.Xlsx).NombreDeArchivo);
    }

    [Fact]
    public void El_creador_rechaza_un_formato_no_soportado()
    {
        var creador = new CreadorExportadorListadoVentas([new ListadoVentasPdf(), new ListadoVentasExcel()]);

        Assert.Throws<FormatoDeExportacionNoSoportadoException>(
            () => creador.Exportar(ReporteConDatos(), FormatoDeExportacion.Json));
    }
}
