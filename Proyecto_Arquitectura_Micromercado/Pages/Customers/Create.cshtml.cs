using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Arquitectura_Micromercado.Application.Common;
using Proyecto_Arquitectura_Micromercado.Application.Customers;
using Proyecto_Arquitectura_Micromercado.Application.Security;

namespace Proyecto_Arquitectura_Micromercado.Pages.Customers;

[Authorize(Roles = Roles.AdministradorYCajero)]
public sealed class CreateModel(IClienteCreacionService servicio) : PageModel
{
    [BindProperty]
    public ClienteCreacionDto Input { get; set; } = new();

    [TempData]
    public string? Confirmacion { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return Page();
        try
        {
            await servicio.CrearAsync(Input, cancellationToken);
            Confirmacion = "Cliente registrado correctamente.";
            return RedirectToPage();
        }
        catch (ClienteDuplicadoException ex)
        {
            ModelState.AddModelError("Input.NitCi", ex.Message);
        }
        catch (ValidationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (ErrorDePersistenciaException)
        {
            ModelState.AddModelError(string.Empty, "No se pudo registrar el cliente. Intente nuevamente.");
        }
        return Page();
    }
}
