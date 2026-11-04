using PuntoMarket.Reportes.Application.Common;
using PuntoMarket.Reportes.Application.Ports.Exportacion;
using PuntoMarket.Reportes.Domain.ListadoVentas;

namespace PuntoMarket.Reportes.Infrastructure.Exportacion.ListadoVentas;

public class CreadorExportadorListadoVentas : CreadorDeExportador<ReporteListadoVentas>
{
    private readonly IReadOnlyList<IExportadorDeReporte<ReporteListadoVentas>> _exportadores;

    public CreadorExportadorListadoVentas(IEnumerable<IExportadorDeReporte<ReporteListadoVentas>> exportadores)
    {
        _exportadores = exportadores.ToList();
    }

    protected override IExportadorDeReporte<ReporteListadoVentas>? CrearExportador(FormatoDeExportacion formato) =>
        _exportadores.FirstOrDefault(exportador => exportador.Formato == formato);
}
