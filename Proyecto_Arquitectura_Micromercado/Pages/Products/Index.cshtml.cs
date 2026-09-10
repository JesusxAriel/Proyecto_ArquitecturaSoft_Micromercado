using MySql.Data.MySqlClient;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Domain.Products;

namespace Proyecto_Arquitectura_Micromercado.Pages.Products;

public sealed class IndexModel(IProductService productService) : PageModel
{
    public IReadOnlyList<ProductListItem> Products { get; private set; } = [];
    public IReadOnlyList<LookupOption> Categories { get; private set; } = [];
    public IReadOnlyList<LookupOption> Suppliers { get; private set; } = [];
    [BindProperty]
    public Product EditProduct { get; set; } = new();
    [BindProperty]
    public int DeleteProductId { get; set; }
    [BindProperty]
    public string DeleteProductName { get; set; } = string.Empty;
    [BindProperty]
    public string DeleteConfirmation { get; set; } = string.Empty;
    public string? DatabaseWarning { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        try
        {
            Products = await productService.GetAllAsync(cancellationToken);
            Categories = await productService.GetCategoriesAsync(cancellationToken);
            Suppliers = await productService.GetSuppliersAsync(cancellationToken);
        }
        catch (MySqlException)
        {
            Products = [];
            DatabaseWarning = "No se pudo conectar con la base de datos. No hay productos para mostrar.";
        }
    }

    public async Task<IActionResult> OnPostEditAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await ReloadProductsAsync(cancellationToken);
            return Page();
        }

        if (!await productService.UpdateAsync(EditProduct, cancellationToken))
        {
            return NotFound();
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(CancellationToken cancellationToken)
    {
        if (DeleteProductId <= 0 || !string.Equals(DeleteProductName, DeleteConfirmation, StringComparison.Ordinal))
        {
            return BadRequest("La confirmación del nombre no coincide.");
        }

        if (!await productService.SoftDeleteAsync(DeleteProductId, cancellationToken))
        {
            return NotFound();
        }

        return RedirectToPage();
    }

    private async Task ReloadProductsAsync(CancellationToken cancellationToken)
    {
        try
        {
            Products = await productService.GetAllAsync(cancellationToken);
            Categories = await productService.GetCategoriesAsync(cancellationToken);
            Suppliers = await productService.GetSuppliersAsync(cancellationToken);
        }
        catch (MySqlException)
        {
            Products = [];
            Categories = [];
            Suppliers = [];
            DatabaseWarning = "No se pudo conectar con la base de datos.";
        }
    }
}
