namespace PuntoMarket.Ventas.Application.Common;

public interface IDetectorDeInterbloqueo
{
    bool EsInterbloqueo(Exception excepcion);
}
