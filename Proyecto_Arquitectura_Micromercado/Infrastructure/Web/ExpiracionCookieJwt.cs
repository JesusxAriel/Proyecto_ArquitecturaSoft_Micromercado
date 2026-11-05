using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Proyecto_Arquitectura_Micromercado.Application.Security;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Web;

public static class ExpiracionCookieJwt
{
    public static Task LimitarExpiracionAsync(CookieSigningInContext contexto)
    {
        var claimExpiracion = contexto.Principal?.FindFirst(ClaimsDeIdentidad.ExpiracionJwt);
        if (claimExpiracion is null)
            return Task.CompletedTask;

        if (!long.TryParse(
                claimExpiracion.Value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var segundosUnix))
        {
            throw new InvalidOperationException("La cookie contiene una expiración JWT inválida.");
        }

        DateTimeOffset expiracionJwt;
        try
        {
            expiracionJwt = DateTimeOffset.FromUnixTimeSeconds(segundosUnix);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new InvalidOperationException(
                "La cookie contiene una expiración JWT inválida.",
                exception);
        }

        contexto.Properties.ExpiresUtc = CalcularExpiracion(
            DateTimeOffset.UtcNow,
            contexto.Options.ExpireTimeSpan,
            expiracionJwt,
            contexto.Properties.ExpiresUtc);

        return Task.CompletedTask;
    }

    public static DateTimeOffset CalcularExpiracion(
        DateTimeOffset ahoraUtc,
        TimeSpan duracionMaximaCookie,
        DateTimeOffset expiracionJwtUtc,
        DateTimeOffset? expiracionExistenteUtc = null)
    {
        var limiteCookie = ahoraUtc.Add(duracionMaximaCookie);
        if (expiracionExistenteUtc is DateTimeOffset existente &&
            existente < limiteCookie)
        {
            limiteCookie = existente;
        }

        return expiracionJwtUtc < limiteCookie ? expiracionJwtUtc : limiteCookie;
    }
}
