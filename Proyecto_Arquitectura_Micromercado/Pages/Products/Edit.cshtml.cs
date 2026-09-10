using Microsoft.AspNetCore.Mvc;
using Proyecto_Arquitectura_Micromercado.Application.Products;

namespace Proyecto_Arquitectura_Micromercado.Pages.Products;

public sealed class EditModel(IProductService productService) : ProductFormModel(productService)
{
    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var product = await ProductService.GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        Product = product;
        SetPriceInputs(Product, Input);
        await LoadLookupsAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var pricesValid = TryParsePrices();
        if (Product.Id <= 0)
        {
            ModelState.AddModelError(nameof(Product.Id), "El producto no es válido.");
        }

        if (!pricesValid || !ModelState.IsValid)
        {
            await LoadLookupsAsync(cancellationToken);
            return Page();
        }

        if (!await ProductService.UpdateAsync(Product, cancellationToken))
        {
            return NotFound();
        }

        StatusMessage = "Producto actualizado correctamente.";
        return RedirectToPage("/Products/Index");
    }
}
