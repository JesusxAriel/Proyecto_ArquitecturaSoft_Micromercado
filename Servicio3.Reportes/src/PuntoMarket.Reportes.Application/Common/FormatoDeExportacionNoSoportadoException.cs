namespace PuntoMarket.Reportes.Application.Common;

public class FormatoDeExportacionNoSoportadoException : Exception
{
    public FormatoDeExportacionNoSoportadoException(FormatoDeExportacion formato)
        : base($"El formato de exportación {formato} no está soportado para este reporte.")
    {
    }
}
