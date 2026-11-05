using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PuntoMarket.Ventas.Frontend.Clients;
using PuntoMarket.Ventas.Frontend.Security;

namespace PuntoMarket.Ventas.Frontend.Pages.Users;

[AllowAnonymous]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ActivarModel(IActivacionApiClient api, ActivacionesPendientes pendientes) : PageModel
{
    private const string Cookie = "PuntoMarket.Activacion";
    public string? ErrorActivacion { get; private set; }
    public bool PuedeActivar { get; private set; }
    public bool Completada { get; private set; }
    private CookieOptions Opciones() => new() {
        // El enlace llega desde correo/webmail (navegación cross-site). Strict omite esta
        // referencia en el GET de la redirección; Lax la permite solo al navegar con GET.
        // El POST sigue protegido por antiforgery; las cookies de login no cambian.
        HttpOnly = true, Secure = Request.IsHttps, SameSite = SameSiteMode.Lax,
        Path = Request.PathBase + "/Users/Activar", IsEssential = true, MaxAge = TimeSpan.FromMinutes(15)
    };

    public IActionResult OnGet()
    {
        if (Request.Query.ContainsKey("token"))
        {
            var token = Request.Query["token"];
            Limpiar();
            if (token.Count == 1 && !string.IsNullOrWhiteSpace(token[0]) && token[0]!.Length <= 4096)
                Response.Cookies.Append(Cookie, pendientes.Guardar(token[0]!), Opciones());
            // Nunca renderizar el enlace original ni trasladar el token al formulario.
            return RedirectToPage("Activar");
        }
        Completada = TempData["CuentaActivada"] is true;
        PuedeActivar = !Completada && pendientes.Obtener(Request.Cookies[Cookie]) is not null;
        if (!Completada && !PuedeActivar) FalloEnlace();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var token = pendientes.Obtener(Request.Cookies[Cookie]);
        if (token is null) { FalloEnlace(); return Page(); }
        PuedeActivar = true;
        // Lectura explícita: no conservar valores intentados de contraseñas en ModelState/HTML.
        var form = await Request.ReadFormAsync(ct);
        string? nueva = form["NuevaContrasena"].ToString();
        string? confirmar = form["ConfirmarContrasena"].ToString();
        try
        {
            if (string.IsNullOrWhiteSpace(nueva) || nueva.Length < 8 || !nueva.Any(char.IsUpper) ||
                !nueva.Any(char.IsLower) || !nueva.Any(char.IsDigit) || !nueva.Any(c => !char.IsLetterOrDigit(c)))
            {
                ErrorActivacion = "La contraseña debe tener al menos 8 caracteres, mayúscula, minúscula, número y un carácter no alfanumérico.";
                Response.StatusCode = 400; return Page();
            }
            if (!string.Equals(nueva, confirmar, StringComparison.Ordinal))
            {
                ErrorActivacion = "Las contraseñas no coinciden."; Response.StatusCode = 400; return Page();
            }
            await api.Activar(token, nueva, confirmar, ct);
            Limpiar();
            TempData["CuentaActivada"] = true;
            return RedirectToPage("Activar");
        }
        catch (ActivacionApiException e)
        {
            Limpiar(); PuedeActivar = false; ErrorActivacion = e.Message;
            Response.StatusCode = e.Estado; return Page();
        }
        finally { nueva = null; confirmar = null; token = null; }
    }
    private void FalloEnlace()
    {
        ErrorActivacion = "Abra un enlace de activación válido y reciente enviado por PuntoMarket. Si expiró, solicite uno nuevo al administrador.";
        Response.StatusCode = 400;
    }
    private void Limpiar()
    {
        pendientes.Eliminar(Request.Cookies[Cookie]);
        Response.Cookies.Delete(Cookie, Opciones());
    }
}
