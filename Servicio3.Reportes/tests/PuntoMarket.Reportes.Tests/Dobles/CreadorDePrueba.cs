using PuntoMarket.Reportes.Application.Common;
using PuntoMarket.Reportes.Application.Ports.Exportacion;

namespace PuntoMarket.Reportes.Tests.Dobles;

public class CreadorDePrueba : CreadorDeExportador<string>
{
    private readonly IReadOnlyList<IExportadorDeReporte<string>> _exportadores;

    public CreadorDePrueba(params IExportadorDeReporte<string>[] exportadores)
    {
        _exportadores = exportadores;
    }

    protected override IExportadorDeReporte<string>? CrearExportador(FormatoDeExportacion formato) =>
        _exportadores.FirstOrDefault(e => e.Formato == formato);
}
