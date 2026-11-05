using PuntoMarket.Reportes.Application.Common;
using PuntoMarket.Reportes.Application.Ports.Exportacion;
using PuntoMarket.Reportes.Domain.VentasPorCategoria;
using PuntoMarket.Reportes.Infrastructure.Exportacion.Comunes;
using QuestPDF.Fluent;

namespace PuntoMarket.Reportes.Infrastructure.Exportacion.VentasPorCategoria;

public class VentasPorCategoriaPdf : IExportadorDeReporte<ReporteVentasPorCategoria>
{
    private const string TipoPdf = "application/pdf";

    private readonly GeneradorDeGrafico _generadorDeGrafico;

    public VentasPorCategoriaPdf(GeneradorDeGrafico generadorDeGrafico)
    {
        LicenciaDeQuestPdf.Configurar();
        _generadorDeGrafico = generadorDeGrafico;
    }

    public FormatoDeExportacion Formato => FormatoDeExportacion.Pdf;

    public ArchivoGenerado Exportar(ReporteVentasPorCategoria reporte)
    {
        var documento = new VentasPorCategoriaDocumento(reporte, GenerarGrafico(reporte), LogoDelReporte.Obtener());

        return new ArchivoGenerado(documento.GeneratePdf(), TipoPdf, NombreDeArchivo(reporte));
    }

    private byte[]? GenerarGrafico(ReporteVentasPorCategoria reporte) =>
        reporte.Filas.Count == 0 ? null : _generadorDeGrafico.GenerarBarras(reporte.Filas);

    private static string NombreDeArchivo(ReporteVentasPorCategoria reporte) =>
        $"ventas-por-categoria-{reporte.GeneradoEn:yyyyMMdd-HHmmss}.pdf";
}
