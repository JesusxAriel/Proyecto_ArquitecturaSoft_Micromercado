using PuntoMarket.Reportes.Application.Common;

namespace PuntoMarket.Reportes.Application.Ports.Exportacion;

public interface IExportadorDeReporte<in TReporte>
{
    FormatoDeExportacion Formato { get; }

    ArchivoGenerado Exportar(TReporte reporte);
}
