using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace PuntoMarket.Ventas.Frontend.Pages.Demo;
[AllowAnonymous]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class IndexModel : PageModel
{
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }
    private IActionResult Redirigir() => string.IsNullOrEmpty(ReturnUrl) ? RedirectToPage("/Account/Login") :
        Url.IsLocalUrl(ReturnUrl) ? RedirectToPage("/Account/Login", new { ReturnUrl }) : BadRequest();
    public IActionResult OnGet() => Redirigir();
    public IActionResult OnPost() => Redirigir();
    public IActionResult OnPostAdministrador() => Redirigir();
    public IActionResult OnPostSalir() => Redirigir();
}
