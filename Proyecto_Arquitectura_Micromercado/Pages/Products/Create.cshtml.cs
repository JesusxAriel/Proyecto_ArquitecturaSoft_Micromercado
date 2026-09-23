using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Domain.Products;

namespace Proyecto_Arquitectura_Micromercado.Pages.Products;

public sealed class CreateModel(IProductService productService) : ProductFormModel(productService)
{
    [TempData]
    public string? StatusMessage { get; set; }

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

        try
        {
            await ProductService.CreateAsync(Product, cancellationToken);
            StatusMessage = "Producto creado correctamente.";
            return RedirectToPage("/Products/Index");
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadLookupsAsync(cancellationToken);
            return Page();
        }
        catch (MySqlException)
        {
            ModelState.AddModelError(string.Empty, "No se pudo guardar el producto por un problema de conexión.");
            await LoadLookupsAsync(cancellationToken);
            return Page();
        }
    }
}
