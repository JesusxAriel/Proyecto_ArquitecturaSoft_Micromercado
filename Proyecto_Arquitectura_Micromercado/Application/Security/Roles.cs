namespace Proyecto_Arquitectura_Micromercado.Application.Security;

// Nombres de los roles base. Coinciden con ROL.nombre en la base de datos, donde los
// triggers impiden renombrarlos o desactivarlos, asi que son seguros como constantes.
public static class Roles
{
    public const string Administrador = "Administrador";
    public const string Cajero = "Cajero";

    // Para [Authorize(Roles = ...)] en paginas que ambos roles pueden usar.
    public const string AdministradorYCajero = Administrador + "," + Cajero;
}
