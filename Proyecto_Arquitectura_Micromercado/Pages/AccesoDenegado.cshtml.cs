using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Proyecto_Arquitectura_Micromercado.Pages;

[AllowAnonymous]
public sealed class AccesoDenegadoModel : PageModel
{
    public void OnGet()
    {
    }
}
