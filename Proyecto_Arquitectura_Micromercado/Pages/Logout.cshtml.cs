using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Proyecto_Arquitectura_Micromercado.Pages;

[AllowAnonymous]
public sealed class LogoutModel : PageModel
{
    // Entrar por GET no cierra la sesion: asi un enlace o una imagen externa no pueden
    // expulsar al usuario. El cierre solo ocurre por POST, que lleva token antifalsificacion.
    public IActionResult OnGet() => RedirectToPage("/Index");

    public async Task<IActionResult> OnPostAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        HttpContext.Session.Clear();

        return RedirectToPage("/Login");
    }
}
