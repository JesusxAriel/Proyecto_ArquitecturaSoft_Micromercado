using Microsoft.AspNetCore.Mvc;
using Proyecto_Arquitectura_Micromercado.Application.Suppliers;

namespace Proyecto_Arquitectura_Micromercado.Pages.Suppliers;

public sealed class CreateModel(ISupplierService supplierService) : SupplierFormModel(supplierService)
{
    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (!await IsNombreEmpresaDisponibleAsync(cancellationToken))
        {
            return Page();
        }

        await SupplierService.CreateAsync(Supplier, cancellationToken);
        return RedirectToPage("/Suppliers/Index");
    }
}