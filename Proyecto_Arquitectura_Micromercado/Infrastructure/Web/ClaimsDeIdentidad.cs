using System.Security.Claims;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Web;

// Nombres de los claims que viajan en la cookie de autenticacion y utilidades para
// crearlos. Quien emite la cookie (el login) y quien la lee (UsuarioActualDesdeClaims)
// comparten estas constantes, de modo que no puedan desincronizarse.
public static class ClaimsDeIdentidad
{
    public const string IdUsuario = ClaimTypes.NameIdentifier;
    public const string NombreUsuario = ClaimTypes.Name;
    public const string Rol = ClaimTypes.Role;

    public static ClaimsPrincipal CrearPrincipal(int id, string nombreUsuario, string rol, string esquema)
    {
        var identidad = new ClaimsIdentity(
            [
                new Claim(IdUsuario, id.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new Claim(NombreUsuario, nombreUsuario),
                new Claim(Rol, rol)
            ],
            esquema);

        return new ClaimsPrincipal(identidad);
    }
}
