using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Arquitectura_Micromercado.Application.Common;
using Proyecto_Arquitectura_Micromercado.Application.Customers;
using Proyecto_Arquitectura_Micromercado.Application.Security;
using Proyecto_Arquitectura_Micromercado.Pages.Shared;

namespace Proyecto_Arquitectura_Micromercado.Pages.Customers;

[Authorize(Roles = Roles.AdministradorYCajero)]
public sealed class CreateModel(
    IClienteCreacionService servicio,
    IClienteConsultaService clienteConsultaService) : PageModel
{
    [BindProperty]
    public ClienteCreacionDto Input { get; set; } = new();

    [TempData]
    public string? Confirmacion { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnGetCheckUniqueAsync(
        string? nitCi, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        var duplicado = await clienteConsultaService.ExisteNitCiAsync(nitCi ?? string.Empty, cancellationToken);
        return UniqueCheckResult.ToJson(duplicado, new ClienteDuplicadoException().Message);
    }

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
