using System.Security.Claims;

namespace Proyecto_Arquitectura_Micromercado.Application.Security;

public static class ClaimsDeIdentidad
{
    public const string IdUsuario = ClaimTypes.NameIdentifier;
    public const string NombreUsuario = ClaimTypes.Name;
    public const string Rol = ClaimTypes.Role;
}
