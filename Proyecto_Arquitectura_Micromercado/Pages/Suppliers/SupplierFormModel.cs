using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Arquitectura_Micromercado.Application.Suppliers;
using Proyecto_Arquitectura_Micromercado.Domain.Suppliers;

namespace Proyecto_Arquitectura_Micromercado.Pages.Suppliers;

public abstract class SupplierFormModel(ISupplierService supplierService) : PageModel
{
    protected ISupplierService SupplierService { get; } = supplierService;

    [BindProperty]
    public Supplier Supplier { get; set; } = new();

    /// <summary>
    /// Regla que depende de datos ya registrados, por lo que no puede
    /// resolverse con DataAnnotations. Se comparte entre Create y Edit.
    /// </summary>
    protected async Task<bool> IsNombreEmpresaDisponibleAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Supplier.NombreEmpresa))
        {
            return false;
        }

        var yaExiste = await SupplierService.ExistsNombreEmpresaAsync(
            Supplier.NombreEmpresa,
            Supplier.Id,
            cancellationToken);

        if (yaExiste)
        {
            ModelState.AddModelError(
                "Supplier.NombreEmpresa",
                SupplierValidation.NombreDuplicadoMessage);
        }

        return !yaExiste;
    }
}