using MySql.Data.MySqlClient;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Domain.Products;

namespace Proyecto_Arquitectura_Micromercado.Pages.Products;

public sealed class IndexModel(
    IProductService productService,
    ILogger<IndexModel> logger) : PageModel
{
    public IReadOnlyList<ProductListItem> Products { get; private set; } = [];
    public IReadOnlyList<LookupOption> Categories { get; private set; } = [];
    public IReadOnlyList<LookupOption> Suppliers { get; private set; } = [];
    [TempData]
    public string? StatusMessage { get; set; }
    public Product CreateProduct { get; set; } = new();
    [BindProperty]
    public Product EditProduct { get; set; } = new();
    [BindProperty]
    public string? PriceChangeReason { get; set; }
    [BindProperty]
    public int DeleteProductId { get; set; }
    public string? DatabaseWarning { get; private set; }
    public bool ShowCreateModal { get; private set; }
    public bool ShowEditModal { get; private set; }

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

    public async Task<IActionResult> OnPostCreateAsync(
        [FromForm(Name = "CreateProduct")] Product createProduct,
        CancellationToken cancellationToken)
    {
        CreateProduct = createProduct;
        ModelState.Clear();

        if (!TryValidateModel(CreateProduct, nameof(CreateProduct)))
        {
            ShowCreateModal = true;
            await ReloadProductsAsync(cancellationToken);
            return Page();
        }

        try
        {
            await productService.CreateAsync(CreateProduct, cancellationToken);
            StatusMessage = "Producto creado correctamente.";
            return RedirectToPage();
        }
        catch (ArgumentException ex)
        {
            ShowCreateModal = true;
            ModelState.AddModelError(string.Empty, ex.Message);
            await ReloadProductsAsync(cancellationToken);
            return Page();
        }
        catch (MySqlException)
        {
            ShowCreateModal = true;
            DatabaseWarning = "No se pudo guardar el producto por un problema de conexión.";
            await ReloadProductsAsync(cancellationToken);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostEditAsync(CancellationToken cancellationToken)
    {
        RemoveUnrelatedModelState();

        if (!ModelState.IsValid)
        {
            ShowEditModal = true;
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
                if (!IsValidReason(PriceChangeReason))
                {
                    ShowEditModal = true;
                    ModelState.AddModelError(nameof(PriceChangeReason),
                        "Ingresa una justificación de al menos 3 caracteres. Evita etiquetas HTML, comillas y caracteres de control.");
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

            StatusMessage = "Producto actualizado correctamente.";
            return RedirectToPage();
        }
        catch (ArgumentException ex)
        {
            ShowEditModal = true;
            ModelState.AddModelError(string.Empty, ex.Message);
            await ReloadProductsAsync(cancellationToken);
            return Page();
        }
        catch (MySqlException)
        {
            ShowEditModal = true;
            DatabaseWarning = "No se pudo actualizar el producto por un problema de conexión.";
            await ReloadProductsAsync(cancellationToken);
            return Page();
        }
    }

    private void RemoveUnrelatedModelState()
    {
        ModelState.Remove(nameof(PriceChangeReason));
        ModelState.Remove("Product.PriceChangeReason");
    }

    private static string NormalizeReason(string? value)
    {
        var normalized = string.Join(
            " ",
            (value ?? string.Empty).TrimStart().Split(
                [' ', '\t', '\r', '\n'],
                StringSplitOptions.RemoveEmptyEntries));

        if (string.IsNullOrEmpty(normalized))
        {
            return string.Empty;
        }

        var firstLetterIndex = normalized
            .Select((character, index) => (character, index))
            .FirstOrDefault(item => char.IsLetter(item.character))
            .index;

        if (firstLetterIndex == 0 && char.IsLetter(normalized[0]))
        {
            return char.ToUpperInvariant(normalized[0]) + normalized[1..];
        }

        if (firstLetterIndex > 0)
        {
            return normalized[..firstLetterIndex] +
                char.ToUpperInvariant(normalized[firstLetterIndex]) +
                normalized[(firstLetterIndex + 1)..];
        }

        return normalized;
    }

    private static bool IsValidReason(string value)
    {
        return value.Length >= 3 &&
            !value.Any(character =>
                char.IsControl(character) ||
                character is '<' or '>' or '"' or '\'' or '`' or '\\' or ';');
    }

    public async Task<IActionResult> OnPostDeleteAsync(CancellationToken cancellationToken)
    {
        if (DeleteProductId <= 0)
        {
            return BadRequest("El producto no es válido.");
        }

        if (!await productService.SoftDeleteAsync(DeleteProductId, cancellationToken))
        {
            return NotFound();
        }

        StatusMessage = "Producto eliminado correctamente.";
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
