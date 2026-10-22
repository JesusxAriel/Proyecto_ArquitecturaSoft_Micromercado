using System.Globalization;
using System.Security.Claims;
using Proyecto_Arquitectura_Micromercado.Application.Security;

namespace Proyecto_Arquitectura_Micromercado.Pages.Shared;

public static class PrincipalDeSesion
{
    public static ClaimsPrincipal Crear(
        int id,
        string nombreUsuario,
        string rol,
        string esquema,
        bool debeCambiarPassword = false)
    {
        var identidad = new ClaimsIdentity(
            [
                new Claim(ClaimsDeIdentidad.IdUsuario, id.ToString(CultureInfo.InvariantCulture)),
                new Claim(ClaimsDeIdentidad.NombreUsuario, nombreUsuario),
                new Claim(ClaimsDeIdentidad.Rol, rol),
                new Claim(ClaimsDeIdentidad.DebeCambiarPassword, debeCambiarPassword.ToString())
            ],
            esquema);

        return new ClaimsPrincipal(identidad);
    }
}
