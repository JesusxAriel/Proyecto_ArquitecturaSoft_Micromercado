namespace PuntoMarket.Reportes.Application.Common;

public class FiltroDeReporteInvalidoException : Exception
{
    public FiltroDeReporteInvalidoException(string mensaje) : base(mensaje)
    {
    }
}
