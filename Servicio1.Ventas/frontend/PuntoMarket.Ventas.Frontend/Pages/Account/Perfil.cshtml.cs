using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PuntoMarket.Ventas.Frontend.Security;
namespace PuntoMarket.Ventas.Frontend.Pages.Account;

[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PerfilModel : PageModel
{
    public string NombreDeUsuario { get; private set; } = "";
    public string Rol { get; private set; } = "";
    public DateTime? InicioDeSesion { get; private set; }

    public async Task OnGetAsync()
    {
        NombreDeUsuario = User.FindFirst(ClaimTypes.Name)?.Value ?? "";
        Rol = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
        var autenticacion = await HttpContext.AuthenticateAsync(AccesoUsuariosPreparado.Esquema);
        InicioDeSesion = autenticacion.Properties?.IssuedUtc?.LocalDateTime;
    }
}
