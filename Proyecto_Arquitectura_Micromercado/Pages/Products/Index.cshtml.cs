using MySql.Data.MySqlClient;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Domain.Common;
using Proyecto_Arquitectura_Micromercado.Domain.Products;
using Proyecto_Arquitectura_Micromercado.Pages.Shared;

namespace Proyecto_Arquitectura_Micromercado.Pages.Products;

public sealed class IndexModel(
    IProductService productService,
    ILogger<IndexModel> logger) : PageModel
{
    public PagedResult<ProductListItem> PagedProducts { get; private set; } = new();
    public IReadOnlyList<ProductListItem> Products => PagedProducts.Items;
    [BindProperty(SupportsGet = true)]
    public int Pagina { get; set; } = 1;
    [BindProperty(SupportsGet = true)]
    public int Tamano { get; set; } = PageSizes.Default;
    [BindProperty(SupportsGet = true)]
    public string? Q { get; set; }
    public IReadOnlyList<LookupOption> Packagings { get; private set; } = [];
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
            await LoadPageAsync(cancellationToken);
            Packagings = await productService.GetPackagingsAsync(cancellationToken);
            Categories = await productService.GetCategoriesAsync(cancellationToken);
            Suppliers = await productService.GetSuppliersAsync(cancellationToken);
        }
        catch (MySqlException)
        {
            PagedProducts = new PagedResult<ProductListItem>();
            DatabaseWarning = "No se pudo conectar con la base de datos. No hay productos para mostrar.";
        }
    }

    private async Task LoadPageAsync(CancellationToken cancellationToken)
    {
        PagedProducts = await productService.GetPagedAsync(Pagina, Tamano, Q, cancellationToken);
        Pagina = PagedProducts.Page;
        Tamano = PagedProducts.PageSize;
    }

    private object ListRouteValues => new { Pagina, Tamano, Q };

    // Endpoint GET para el script de validación en vivo (site.js).
    public async Task<IActionResult> OnGetCheckUniqueAsync(
        string? nombre,
        int idEmpaque,
        int id,
        CancellationToken cancellationToken)
    {
        var duplicate =
            !string.IsNullOrWhiteSpace(nombre) &&
            idEmpaque > 0 &&
            await productService.IsDuplicateAsync(nombre, idEmpaque, id, cancellationToken);

        return UniqueCheckResult.ToJson(duplicate, ProductValidation.DuplicateMessage);
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
            return RedirectToPage(ListRouteValues);
        }
        catch (ArgumentException ex)
        {
            ShowCreateModal = true;

            // Un duplicado (carrera con otra solicitud) se muestra junto al campo Nombre.
            ModelState.AddModelError(
                ex is DuplicateProductException ? "CreateProduct.Nombre" : string.Empty,
                ex.Message);
            await ReloadProductsAsync(cancellationToken);
            return Page();
        }
        catch (MySqlException)
        {
            ShowCreateModal = true;
            ModelState.AddModelError(string.Empty, "No se pudo guardar el producto. Intente nuevamente.");
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
            // El servicio decide si el cambio de precio exige justificacion, la normaliza
            // y la valida. La pagina solo traslada lo que escribio el usuario.
            EditProduct.MotivoCambio = PriceChangeReason;

            if (!await productService.UpdateAsync(EditProduct, cancellationToken))
            {
                return NotFound();
            }

            StatusMessage = "Producto actualizado correctamente.";
            return RedirectToPage(ListRouteValues);
        }
        catch (ArgumentException ex)
        {
            ShowEditModal = true;

            // Cada error se muestra junto al campo que lo origina; el resto, en el resumen.
            ModelState.AddModelError(
                ex switch
                {
                    DuplicateProductException => "EditProduct.Nombre",
                    MotivoDeCambioInvalidoException => nameof(PriceChangeReason),
                    _ => string.Empty
                },
                ex.Message);

            // El servicio ya normalizo el motivo, asi que el modal lo vuelve a mostrar
            // tal como quedara guardado.
            PriceChangeReason = EditProduct.MotivoCambio;

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
        ModelState.Remove(nameof(Pagina));
        ModelState.Remove(nameof(Tamano));
        ModelState.Remove(nameof(Q));
    }

    public async Task<IActionResult> OnPostDeleteAsync(CancellationToken cancellationToken)
    {
        if (DeleteProductId <= 0)
        {
            return BadRequest("El producto no es válido.");
        }

        try
        {
            if (!await productService.SoftDeleteAsync(DeleteProductId, cancellationToken))
            {
                return NotFound();
            }

            StatusMessage = "Producto eliminado correctamente.";
        }
        catch (InvalidOperationException ex)
        {
            DatabaseWarning = ex.Message;
            await ReloadProductsAsync(cancellationToken);
            return Page();
        }
        catch (MySqlException)
        {
            DatabaseWarning = "No se pudo eliminar el producto debido a un error en la base de datos.";
            await ReloadProductsAsync(cancellationToken);
            return Page();
        }
        return RedirectToPage(ListRouteValues);
    }

    private async Task ReloadProductsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await LoadPageAsync(cancellationToken);
            Packagings = await productService.GetPackagingsAsync(cancellationToken);
            Categories = await productService.GetCategoriesAsync(cancellationToken);
            Suppliers = await productService.GetSuppliersAsync(cancellationToken);
        }
        catch (MySqlException)
        {
            PagedProducts = new PagedResult<ProductListItem>();
            Packagings = [];
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
