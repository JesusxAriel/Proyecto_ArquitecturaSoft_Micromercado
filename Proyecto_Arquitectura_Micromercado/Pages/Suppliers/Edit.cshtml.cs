using Microsoft.AspNetCore.Mvc;
using Proyecto_Arquitectura_Micromercado.Application.Suppliers;

namespace Proyecto_Arquitectura_Micromercado.Pages.Suppliers;

public sealed class EditModel(ISupplierService supplierService) : SupplierFormModel(supplierService)
{
    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var supplier = await SupplierService.GetByIdAsync(id, cancellationToken);
        if (supplier is null)
        {
            return NotFound();
        }

        Supplier = supplier;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (Supplier.Id <= 0)
        {
            ModelState.AddModelError("Supplier.Id", "El proveedor no es válido.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (!await IsNombreEmpresaDisponibleAsync(cancellationToken))
        {
            return Page();
        }

        if (!await SupplierService.UpdateAsync(Supplier, cancellationToken))
        {
            return NotFound();
        }

        return RedirectToPage("/Suppliers/Index");
    }
}