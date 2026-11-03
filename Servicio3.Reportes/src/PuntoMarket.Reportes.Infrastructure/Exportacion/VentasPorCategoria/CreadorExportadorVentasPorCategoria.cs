using PuntoMarket.Reportes.Application.Common;
using PuntoMarket.Reportes.Application.Ports.Exportacion;
using PuntoMarket.Reportes.Domain.VentasPorCategoria;

namespace PuntoMarket.Reportes.Infrastructure.Exportacion.VentasPorCategoria;

public class CreadorExportadorVentasPorCategoria : CreadorDeExportador<ReporteVentasPorCategoria>
{
    private readonly IReadOnlyList<IExportadorDeReporte<ReporteVentasPorCategoria>> _exportadores;

    public CreadorExportadorVentasPorCategoria(IEnumerable<IExportadorDeReporte<ReporteVentasPorCategoria>> exportadores)
    {
        _exportadores = exportadores.ToList();
    }

    protected override IExportadorDeReporte<ReporteVentasPorCategoria>? CrearExportador(FormatoDeExportacion formato) =>
        _exportadores.FirstOrDefault(exportador => exportador.Formato == formato);
}
