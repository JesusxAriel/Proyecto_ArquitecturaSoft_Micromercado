using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Arquitectura_Micromercado.Application.Security;

namespace Proyecto_Arquitectura_Micromercado.Pages.Sales;

[Authorize(Roles = Roles.AdministradorYCajero)]
public sealed class IndexModel : PageModel
{
    public SaleInputModel Input { get; } = new();

    public void OnGet()
    {
        // Bloque 1: estructura visual, sin cargar catálogos ni procesar ventas.
    }
}
