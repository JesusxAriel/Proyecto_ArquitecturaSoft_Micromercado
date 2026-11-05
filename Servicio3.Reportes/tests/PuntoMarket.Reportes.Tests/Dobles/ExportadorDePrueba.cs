using PuntoMarket.Reportes.Application.Common;
using PuntoMarket.Reportes.Application.Ports.Exportacion;

namespace PuntoMarket.Reportes.Tests.Dobles;

public class ExportadorDePrueba : IExportadorDeReporte<string>
{
    public ExportadorDePrueba(FormatoDeExportacion formato)
    {
        Formato = formato;
    }

    public FormatoDeExportacion Formato { get; }

    public ArchivoGenerado Exportar(string reporte) =>
        new([], $"prueba/{Formato}", $"{reporte}.{Formato}");
}
