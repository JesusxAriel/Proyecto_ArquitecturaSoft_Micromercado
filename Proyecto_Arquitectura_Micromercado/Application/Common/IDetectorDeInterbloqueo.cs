namespace Proyecto_Arquitectura_Micromercado.Application.Common;

public interface IDetectorDeInterbloqueo
{
    bool EsInterbloqueo(Exception excepcion);
}
