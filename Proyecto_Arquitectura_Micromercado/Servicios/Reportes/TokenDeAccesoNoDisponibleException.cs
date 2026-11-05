namespace Proyecto_Arquitectura_Micromercado.Servicios.Reportes;

public sealed class TokenDeAccesoNoDisponibleException : InvalidOperationException
{
    public TokenDeAccesoNoDisponibleException()
        : base("No hay un token de acceso disponible para el usuario actual.")
    {
    }
}
