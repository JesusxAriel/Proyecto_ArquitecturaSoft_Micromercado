using MySql.Data.MySqlClient;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Domain.Products;
using System.Text.RegularExpressions;

namespace Proyecto_Arquitectura_Micromercado.Pages.Products;

public sealed class IndexModel(
    IProductService productService,
    ILogger<IndexModel> logger) : PageModel
{
    public IReadOnlyList<ProductListItem> Products { get; private set; } = [];
    public IReadOnlyList<LookupOption> Categories { get; private set; } = [];
    public IReadOnlyList<LookupOption> Suppliers { get; private set; } = [];
    [BindProperty]
    public Product EditProduct { get; set; } = new();
    [BindProperty]
    public string? PriceChangeReason { get; set; }
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
        RemoveUnrelatedModelState();

        if (!ModelState.IsValid)
        {
            LogModelStateErrors();
            await ReloadProductsAsync(cancellationToken);
            return Page();
        }

        try
        {
            var currentProduct = await productService.GetByIdAsync(EditProduct.Id, cancellationToken);
            if (currentProduct is null)
            {
                return NotFound();
            }

            var pricesChanged = currentProduct.PrecioVenta != EditProduct.PrecioVenta ||
                currentProduct.PrecioCosto != EditProduct.PrecioCosto;
            if (pricesChanged)
            {
                PriceChangeReason = NormalizeReason(PriceChangeReason);
                if (string.IsNullOrWhiteSpace(PriceChangeReason) ||
                    !Regex.IsMatch(PriceChangeReason, @"^[a-zA-Z0-9áéíóúÁÉÍÓÚñÑ\s\.,\-]+$") ||
                    !char.IsUpper(PriceChangeReason[0]))
                {
                    ModelState.AddModelError(nameof(PriceChangeReason),
                        "Ingresa una justificación válida para el cambio de precio.");
                    await ReloadProductsAsync(cancellationToken);
                    return Page();
                }

                EditProduct.MotivoCambio = PriceChangeReason;
            }
            else
            {
                PriceChangeReason = null;
                EditProduct.MotivoCambio = null;
            }

            if (!await productService.UpdateAsync(EditProduct, cancellationToken))
            {
                return NotFound();
            }

            return RedirectToPage();
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await ReloadProductsAsync(cancellationToken);
            return Page();
        }
        catch (MySqlException)
        {
            DatabaseWarning = "No se pudo actualizar el producto por un problema de conexión.";
            await ReloadProductsAsync(cancellationToken);
            return Page();
        }
    }

    private void RemoveUnrelatedModelState()
    {
        ModelState.Remove(nameof(DeleteProductName));
        ModelState.Remove(nameof(DeleteConfirmation));
        ModelState.Remove(nameof(PriceChangeReason));
        ModelState.Remove("Product.PriceChangeReason");
    }

    private static string NormalizeReason(string? value)
    {
        var normalized = string.Join(
            " ",
            (value ?? string.Empty).Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));

        return string.IsNullOrEmpty(normalized)
            ? string.Empty
            : char.ToUpper(normalized[0]) + normalized[1..];
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

    private void LogModelStateErrors()
    {
        foreach (var entry in ModelState)
        {
            foreach (var error in entry.Value.Errors)
            {
                logger.LogWarning(
                    "Error de validación al editar producto. Campo: {Field}. Error: {Error}",
                    entry.Key,
                    error.ErrorMessage);
            }
        }
    }
}
