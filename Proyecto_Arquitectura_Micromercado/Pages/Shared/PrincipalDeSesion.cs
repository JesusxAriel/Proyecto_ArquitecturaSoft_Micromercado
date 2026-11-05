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
        bool debeCambiarPassword = false,
        DateTimeOffset? expiracionJwtUtc = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimsDeIdentidad.IdUsuario, id.ToString(CultureInfo.InvariantCulture)),
            new(ClaimsDeIdentidad.NombreUsuario, nombreUsuario),
            new(ClaimsDeIdentidad.Rol, rol),
            new(ClaimsDeIdentidad.DebeCambiarPassword, debeCambiarPassword.ToString())
        };
        if (expiracionJwtUtc is DateTimeOffset expiracion)
        {
            claims.Add(new Claim(
                ClaimsDeIdentidad.ExpiracionJwt,
                expiracion.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)));
        }

        var identidad = new ClaimsIdentity(
            claims,
            esquema);

        return new ClaimsPrincipal(identidad);
    }
}
