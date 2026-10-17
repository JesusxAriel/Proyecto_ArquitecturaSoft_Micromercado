using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Web;

public static class SesionVencidaAjax
{
    public const string Mensaje = "Su sesión venció. Inicie sesión nuevamente para continuar.";

    public static bool EsPeticionDeDatos(HttpRequest peticion)
    {
        var aceptar = peticion.Headers.Accept.ToString();

        return aceptar.Contains("application/json", StringComparison.OrdinalIgnoreCase)
            && !aceptar.Contains("text/html", StringComparison.OrdinalIgnoreCase);
    }

    public static Task ResponderAsync(RedirectContext<CookieAuthenticationOptions> contexto)
    {
        if (!EsPeticionDeDatos(contexto.Request))
        {
            contexto.Response.Redirect(contexto.RedirectUri);
            return Task.CompletedTask;
        }

        contexto.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return contexto.Response.WriteAsJsonAsync(new { exito = false, sesionVencida = true, mensaje = Mensaje });
    }
}
