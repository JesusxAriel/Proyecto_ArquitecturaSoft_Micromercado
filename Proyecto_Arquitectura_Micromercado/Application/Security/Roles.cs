namespace Proyecto_Arquitectura_Micromercado.Application.Security;

public static class Roles
{
    public const string Administrador = "Administrador";
    public const string Cajero = "Cajero";

    public const string AdministradorYCajero = Administrador + "," + Cajero;
}
