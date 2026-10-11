using System.Security.Claims;

namespace Proyecto_Arquitectura_Micromercado.Application.Security;

// Tipos de claim que describen al usuario autenticado.
//
// Viven en Application porque son el contrato entre quien emite la identidad (el login,
// en Pages) y quien la lee para auditar (UsuarioActualDesdeClaims, en Infrastructure):
// ninguna de las dos capas debe conocer a la otra, pero ambas deben usar los mismos
// nombres. Son solo constantes; crear el principal es trabajo de la capa web.
public static class ClaimsDeIdentidad
{
    public const string IdUsuario = ClaimTypes.NameIdentifier;
    public const string NombreUsuario = ClaimTypes.Name;
    public const string Rol = ClaimTypes.Role;
}
