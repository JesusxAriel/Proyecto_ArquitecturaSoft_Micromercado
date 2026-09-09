using Microsoft.AspNetCore.Mvc;
using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Domain.Products;

namespace Proyecto_Arquitectura_Micromercado.Pages.Products;

public sealed class CreateModel(IProductService productService) : ProductFormModel(productService)
{
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Input.PrecioVentaInput = "0,00";
        Input.PrecioCostoInput = "0,00";
        await LoadLookupsAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var pricesValid = TryParsePrices();
        if (!pricesValid || !ModelState.IsValid)
        {
            await LoadLookupsAsync(cancellationToken);
            return Page();
        }

        await ProductService.CreateAsync(Product, cancellationToken);
        return RedirectToPage("/Products/Index");
    }
}
