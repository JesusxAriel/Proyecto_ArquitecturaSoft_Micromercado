using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PuntoMarket.Ventas.Frontend.Security;
namespace PuntoMarket.Ventas.Frontend.Pages.Account;
[AllowAnonymous]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class LogoutModel(SesionesUsuario sesiones) : PageModel
{
    public void OnGet() { }
    public async Task<IActionResult> OnPostAsync()
    {
        var actual = await HttpContext.AuthenticateAsync(AccesoUsuariosPreparado.Esquema);
        sesiones.Cerrar(actual.Principal?.FindFirst(AccesoUsuariosPreparado.ClaimSesion)?.Value);
        await HttpContext.SignOutAsync(AccesoUsuariosPreparado.Esquema);
        return RedirectToPage("/Account/Login");
    }
}
