using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Arquitectura_Micromercado.Application.Security;
using Proyecto_Arquitectura_Micromercado.Pages.Shared;

namespace Proyecto_Arquitectura_Micromercado.Pages.Cuenta;

[Authorize]
public sealed class PerfilModel : PageModel
{
    public string Nombre { get; private set; } = string.Empty;

    public string NombreUsuario { get; private set; } = string.Empty;

    public string Rol { get; private set; } = string.Empty;

    public string? InicioDeSesion { get; private set; }

    public void OnGet()
    {
        NombreUsuario = User.FindFirst(ClaimsDeIdentidad.NombreUsuario)?.Value ?? string.Empty;
        Rol = User.FindFirst(ClaimsDeIdentidad.Rol)?.Value ?? string.Empty;
        Nombre = HttpContext.Session.GetString(VariablesDeSesion.NombreParaMostrar) ?? NombreUsuario;
        InicioDeSesion = HttpContext.Session.GetString(VariablesDeSesion.InicioDeSesion);
    }
}
