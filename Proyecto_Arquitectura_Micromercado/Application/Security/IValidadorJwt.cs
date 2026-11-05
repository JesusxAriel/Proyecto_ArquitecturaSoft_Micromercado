using System.Security.Claims;

namespace Proyecto_Arquitectura_Micromercado.Application.Security;

public interface IValidadorJwt
{
    ClaimsPrincipal Validar(string token);

    JwtValidado ValidarConExpiracion(string token);
}
