using PuntoMarket.Reportes.Application.Common;

namespace PuntoMarket.Reportes.Application.Ports.Exportacion;

public abstract class CreadorDeExportador<TReporte>
{
    protected abstract IExportadorDeReporte<TReporte>? CrearExportador(FormatoDeExportacion formato);

    public ArchivoGenerado Exportar(TReporte reporte, FormatoDeExportacion formato)
    {
        var exportador = CrearExportador(formato)
            ?? throw new FormatoDeExportacionNoSoportadoException(formato);

        return exportador.Exportar(reporte);
    }
}
