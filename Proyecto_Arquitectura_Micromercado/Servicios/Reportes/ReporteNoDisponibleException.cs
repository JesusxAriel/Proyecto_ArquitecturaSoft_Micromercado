namespace Proyecto_Arquitectura_Micromercado.Servicios.Reportes;

public sealed class ReporteNoDisponibleException : Exception
{
    public ReporteNoDisponibleException(string mensaje) : base(mensaje)
    {
    }

    public ReporteNoDisponibleException(string mensaje, Exception causa) : base(mensaje, causa)
    {
    }
}
