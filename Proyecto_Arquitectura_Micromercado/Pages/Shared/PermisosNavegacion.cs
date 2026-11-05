using System.Security.Claims;
using Proyecto_Arquitectura_Micromercado.Application.Security;

namespace Proyecto_Arquitectura_Micromercado.Pages.Shared;

public sealed record PermisosNavegacion(
    bool PuedeAdministrar,
    bool PuedeAccederCajaVentas,
    bool PuedeConsultarCatalogo)
{
    public static PermisosNavegacion Desde(ClaimsPrincipal usuario)
    {
        ArgumentNullException.ThrowIfNull(usuario);

        var esAdministrador = usuario.IsInRole(Roles.Administrador);
        var esCajero = usuario.IsInRole(Roles.Cajero);
        return new PermisosNavegacion(
            esAdministrador,
            esAdministrador || esCajero,
            esCajero);
    }
}
