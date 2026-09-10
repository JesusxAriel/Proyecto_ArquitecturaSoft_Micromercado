using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Domain.Products;

namespace Proyecto_Arquitectura_Micromercado.Pages.Products;

public sealed class DeleteModel(IProductService productService) : PageModel
{
    [TempData]
    public string? StatusMessage { get; set; }

    [BindProperty]
    public Product Product { get; set; } = new();
    [BindProperty]
    public string ConfirmationName { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var product = await productService.GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        Product = product;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (Product.Id <= 0 || !string.Equals(Product.Nombre, ConfirmationName, StringComparison.Ordinal))
        {
            ModelState.AddModelError(nameof(ConfirmationName), "El nombre de confirmación no coincide exactamente.");
            return Page();
        }

        if (!await productService.SoftDeleteAsync(Product.Id, cancellationToken))
        {
            return NotFound();
        }

        StatusMessage = "Producto eliminado correctamente.";
        return RedirectToPage("/Products/Index");
    }
}
