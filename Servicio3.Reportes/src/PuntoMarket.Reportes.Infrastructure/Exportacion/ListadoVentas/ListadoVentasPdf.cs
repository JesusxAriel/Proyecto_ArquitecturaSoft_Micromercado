using PuntoMarket.Reportes.Application.Common;
using PuntoMarket.Reportes.Application.Ports.Exportacion;
using PuntoMarket.Reportes.Domain.ListadoVentas;
using PuntoMarket.Reportes.Infrastructure.Exportacion.Comunes;
using QuestPDF.Fluent;

namespace PuntoMarket.Reportes.Infrastructure.Exportacion.ListadoVentas;

public class ListadoVentasPdf : IExportadorDeReporte<ReporteListadoVentas>
{
    private const string TipoPdf = "application/pdf";

    public ListadoVentasPdf()
    {
        LicenciaDeQuestPdf.Configurar();
    }

    public FormatoDeExportacion Formato => FormatoDeExportacion.Pdf;

    public ArchivoGenerado Exportar(ReporteListadoVentas reporte)
    {
        var documento = new ListadoVentasDocumento(reporte, LogoDeReportes.Obtener());

        return new ArchivoGenerado(documento.GeneratePdf(), TipoPdf, NombreDeArchivo(reporte));
    }

    private static string NombreDeArchivo(ReporteListadoVentas reporte) =>
        $"listado-de-ventas-{reporte.GeneradoEn:yyyyMMdd-HHmmss}.pdf";
}
