using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
namespace PuntoMarket.Ventas.Frontend.Security;

public static class AccesoUsuariosPreparado
{
    public const string Esquema = "UsuariosPreparado";
    public const string Cookie = "PuntoMarket.Usuarios.Session";
    public const string ClaimSesion = "usuario_session";
    public static IServiceCollection AgregarAccesoUsuariosPreparado(this IServiceCollection servicios)
    {
        // Único esquema predeterminado. No existe ningún esquema de perfiles Demo.
        servicios.AddAuthentication(Esquema).AddCookie(Esquema, o => {
            o.Cookie.Name = Cookie; o.Cookie.HttpOnly = true; o.Cookie.SameSite = SameSiteMode.Strict;
            o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            o.LoginPath = "/Account/Login"; o.AccessDeniedPath = "/Account/Login";
            o.SlidingExpiration = false;
            o.Events.OnRedirectToLogin = c => {
                c.Response.Headers["X-PuntoMarket-Login"] = "/Account/Login";
                if (!c.Request.Query.ContainsKey("handler")) { c.Response.Redirect(c.RedirectUri); return Task.CompletedTask; }
                c.Response.StatusCode = 401;
                return c.Response.WriteAsJsonAsync(new { mensaje = "La sesión no es válida. Inicie sesión nuevamente." });
            };
            o.Events.OnRedirectToAccessDenied = c => {
                c.Response.StatusCode = 403;
                return c.Response.WriteAsJsonAsync(new { mensaje = "No tiene permiso para esta consulta." });
            };
            o.Events.OnValidatePrincipal = async c => {
                var sesiones = c.HttpContext.RequestServices.GetRequiredService<SesionesUsuario>();
                var reloj = c.HttpContext.RequestServices.GetRequiredService<TimeProvider>();
                var p = c.Principal;
                string? Unico(string tipo) { var valores=p?.FindAll(tipo).ToArray(); return valores?.Length == 1 ? valores[0].Value : null; }
                var id = Unico(ClaimSesion);
                var identidad = sesiones.Identidad(id);
                if (identidad is null || p!.HasClaim(x => x.Type == "demo_session") ||
                    p?.Identity?.AuthenticationType != Esquema ||
                    Unico(ClaimTypes.NameIdentifier) != identidad.Id.ToString(CultureInfo.InvariantCulture) ||
                    Unico(ClaimTypes.Name) != identidad.Usuario || Unico(ClaimTypes.Role) != identidad.Rol ||
                    !string.Equals(Unico("debeCambiarPassword"), identidad.DebeCambiarPassword.ToString(), StringComparison.OrdinalIgnoreCase) ||
                    c.Properties.ExpiresUtc is not { } exp || exp > identidad.VenceEn || exp <= reloj.GetUtcNow() ||
                    c.Properties.AllowRefresh != false || c.Properties.IsPersistent)
                {
                    c.RejectPrincipal();
                    await c.HttpContext.SignOutAsync(Esquema);
                }
                else c.ShouldRenew = false;
            };
        });
        servicios.AddOptions<CookieAuthenticationOptions>(Esquema).Configure<TimeProvider>((o,reloj) => o.TimeProvider = reloj);
        return servicios;
    }
}
