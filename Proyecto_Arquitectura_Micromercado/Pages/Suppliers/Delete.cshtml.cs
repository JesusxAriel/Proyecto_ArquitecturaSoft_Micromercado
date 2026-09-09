using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Arquitectura_Micromercado.Application.Suppliers;
using Proyecto_Arquitectura_Micromercado.Domain.Suppliers;

namespace Proyecto_Arquitectura_Micromercado.Pages.Suppliers;

public sealed class DeleteModel(ISupplierService supplierService) : PageModel
{
    [BindProperty]
    public Supplier Supplier { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var supplier = await supplierService.GetByIdAsync(id, cancellationToken);
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
            return BadRequest();
        }

        if (!await supplierService.SoftDeleteAsync(Supplier.Id, cancellationToken))
        {
            return NotFound();
        }

        return RedirectToPage("/Suppliers/Index");
    }
}