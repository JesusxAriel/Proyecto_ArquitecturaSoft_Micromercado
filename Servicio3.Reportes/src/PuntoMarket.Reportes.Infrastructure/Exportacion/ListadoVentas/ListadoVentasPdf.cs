using PuntoMarket.Reportes.Application.Common;
using PuntoMarket.Reportes.Application.Ports.Exportacion;
using PuntoMarket.Reportes.Domain.ListadoVentas;
using PuntoMarket.Reportes.Infrastructure.Exportacion.Comunes;
using PuntoMarket.Reportes.Infrastructure.Exportacion.VentasPorCategoria;
using QuestPDF.Fluent;

namespace PuntoMarket.Reportes.Infrastructure.Exportacion.ListadoVentas;

public class ListadoVentasPdf : IExportadorDeReporte<ReporteListadoVentas>
{
    private const string TipoPdf = "application/pdf";

    private readonly GeneradorDeGrafico _generadorDeGrafico;

    public ListadoVentasPdf(GeneradorDeGrafico generadorDeGrafico)
    {
        LicenciaDeQuestPdf.Configurar();
        _generadorDeGrafico = generadorDeGrafico;
    }

    public FormatoDeExportacion Formato => FormatoDeExportacion.Pdf;

    public ArchivoGenerado Exportar(ReporteListadoVentas reporte)
    {
        var graficos = GraficosDeListadoVentas.Crear(_generadorDeGrafico, reporte);
        var documento = new ListadoVentasDocumento(reporte, graficos, LogoDeReportes.Obtener());

        return new ArchivoGenerado(documento.GeneratePdf(), TipoPdf, NombreDeArchivo(reporte));
    }

    private static string NombreDeArchivo(ReporteListadoVentas reporte) =>
        $"listado-de-ventas-{reporte.GeneradoEn:yyyyMMdd-HHmmss}.pdf";
}
