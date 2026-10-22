using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Proyecto_Arquitectura_Micromercado.Application.Security;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Web;

public static class ValidacionCookieUsuarioActivo
{
    public static async Task ValidarAsync(CookieValidatePrincipalContext contexto)
    {
        var usuarios = contexto.HttpContext.RequestServices.GetRequiredService<IUsuarioRepository>();
        if (await EstaActivaAsync(
                contexto.Principal,
                usuarios,
                contexto.HttpContext.RequestAborted))
        {
            return;
        }

        contexto.RejectPrincipal();
        await contexto.HttpContext.SignOutAsync(contexto.Scheme.Name);
    }

    public static Task<bool> EstaActivaAsync(
        System.Security.Claims.ClaimsPrincipal? principal,
        IUsuarioRepository usuarios,
        CancellationToken cancellationToken = default)
    {
        var idClaim = principal?.FindFirst(ClaimsDeIdentidad.IdUsuario)?.Value;
        if (!int.TryParse(idClaim, NumberStyles.None, CultureInfo.InvariantCulture, out var usuarioId)
            || usuarioId <= 0)
        {
            return Task.FromResult(false);
        }

        return usuarios.EstaActivoAsync(usuarioId, cancellationToken);
    }
}
