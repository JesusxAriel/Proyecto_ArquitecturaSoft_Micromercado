using System.Globalization;
using System.Security.Claims;
using Proyecto_Arquitectura_Micromercado.Application.Security;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Web;

// Adaptador del puerto IUsuarioActual para la aplicacion web.
//
// Lee SIEMPRE de los claims de HttpContext.User, que viajan firmados en la cookie de
// autenticacion. Nunca consulta la Session: sus variables las puede cambiar la propia
// aplicacion en cualquier momento y no sirven como identidad de auditoria.
public sealed class UsuarioActualDesdeClaims(IHttpContextAccessor accesor) : IUsuarioActual
{
    public int Id
    {
        get
        {
            var valor = Claim(ClaimsDeIdentidad.IdUsuario);

            return int.TryParse(valor, NumberStyles.None, CultureInfo.InvariantCulture, out var id)
                ? id
                : throw SinUsuario();
        }
    }

    public string NombreUsuario => Claim(ClaimsDeIdentidad.NombreUsuario) ?? throw SinUsuario();

    public string Rol => Claim(ClaimsDeIdentidad.Rol) ?? throw SinUsuario();

    private string? Claim(string tipo)
    {
        var usuario = accesor.HttpContext?.User;

        return usuario?.Identity?.IsAuthenticated == true
            ? usuario.FindFirstValue(tipo)
            : null;
    }

    private static InvalidOperationException SinUsuario() =>
        new("No hay un usuario autenticado en la peticion actual.");
}
