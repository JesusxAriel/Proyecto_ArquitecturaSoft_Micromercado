using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Arquitectura_Micromercado.Application.Common;
using Proyecto_Arquitectura_Micromercado.Application.Suppliers;
using Proyecto_Arquitectura_Micromercado.Domain.Common;
using Proyecto_Arquitectura_Micromercado.Domain.Suppliers;
using Proyecto_Arquitectura_Micromercado.Pages.Shared;

using Proyecto_Arquitectura_Micromercado.Application.Products;

namespace Proyecto_Arquitectura_Micromercado.Pages.Suppliers;

public sealed class IndexModel(
    ISupplierService supplierService,
    IProductService productService,
    ILogger<IndexModel> logger) : PageModel
{
    public PagedResult<SupplierListItem> PagedSuppliers { get; private set; } = new();
    public IReadOnlyList<SupplierListItem> Suppliers => PagedSuppliers.Items;

    [BindProperty(SupportsGet = true)]
    public int Pagina { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int Tamano { get; set; } = PageSizes.Default;

    [BindProperty(SupportsGet = true)]
    public string? Q { get; set; }

    private object ListRouteValues => new { Pagina, Tamano, Q };
    public string? DatabaseWarning { get; private set; }
    [TempData]
    public string? ErrorMessage { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public Supplier CreateSupplier { get; set; } = new();

    public Supplier EditSupplier { get; set; } = new();

    [BindProperty]
    public int DeleteSupplierId { get; set; }

    public bool ShowCreateModal { get; private set; }
    public bool ShowEditModal { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadSuppliersAsync(cancellationToken);
    }

    // Endpoint GET para el script de validación en vivo (site.js).
    public async Task<IActionResult> OnGetCheckUniqueAsync(
        string? nombre,
        int id,
        CancellationToken cancellationToken)
    {
        var duplicate =
            !string.IsNullOrWhiteSpace(nombre) &&
            await supplierService.ExistsNombreEmpresaAsync(nombre, id, cancellationToken);

        return UniqueCheckResult.ToJson(duplicate, SupplierValidation.NombreDuplicadoMessage);
    }

    public async Task<IActionResult> OnPostCreateAsync(
        [FromForm] Supplier createSupplier,
        CancellationToken cancellationToken)
    {
        CreateSupplier = createSupplier;
        RemoveDeleteModelState();

        if (!ModelState.IsValid)
        {
            LogModelStateErrors("crear");
            ShowCreateModal = true;
            await LoadSuppliersAsync(cancellationToken);
            return Page();
        }

        try
        {
            await supplierService.CreateAsync(CreateSupplier, cancellationToken);
            StatusMessage = "Proveedor creado correctamente.";
            return RedirectToPage(ListRouteValues);
        }
        catch (ArgumentException ex)
        {
            // Un duplicado (carrera con otra solicitud) se muestra junto al campo Nombre.
            ModelState.AddModelError(
                ex is DuplicateSupplierException ? "CreateSupplier.NombreEmpresa" : string.Empty,
                ex.Message);
            ShowCreateModal = true;
            await LoadSuppliersAsync(cancellationToken);
            return Page();
        }
        catch (ErrorDePersistenciaException)
        {
            DatabaseWarning = "No se pudo guardar el proveedor por un problema de conexión.";
            ShowCreateModal = true;
            await LoadSuppliersAsync(cancellationToken);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostEditAsync(
        [FromForm] Supplier editSupplier,
        CancellationToken cancellationToken)
    {
        EditSupplier = editSupplier;
        RemoveDeleteModelState();

        if (!ModelState.IsValid || EditSupplier.Id <= 0)
        {
            LogModelStateErrors();
            ShowEditModal = true;
            await LoadSuppliersAsync(cancellationToken);
            return Page();
        }

        try
        {
            if (!await supplierService.UpdateAsync(EditSupplier, cancellationToken))
            {
                return NotFound();
            }

            StatusMessage = "Proveedor actualizado correctamente.";
            return RedirectToPage(ListRouteValues);
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(
                ex is DuplicateSupplierException ? "EditSupplier.NombreEmpresa" : string.Empty,
                ex.Message);
            ShowEditModal = true;
            await LoadSuppliersAsync(cancellationToken);
            return Page();
        }
        catch (ErrorDePersistenciaException)
        {
            DatabaseWarning = "No se pudo actualizar el proveedor por un problema de conexión.";
            ShowEditModal = true;
            await LoadSuppliersAsync(cancellationToken);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(CancellationToken cancellationToken)
    {
        if (DeleteSupplierId <= 0)
        {
            return BadRequest("El proveedor no es válido.");
        }

        if (await TieneProductosActivosAsync(DeleteSupplierId, cancellationToken))
        {
            ErrorMessage = "No se puede eliminar el proveedor porque tiene productos asociados.";
            return RedirectToPage(ListRouteValues);
        }

        if (!await supplierService.SoftDeleteAsync(DeleteSupplierId, cancellationToken))
        {
            return NotFound();
        }

        StatusMessage = "Proveedor eliminado correctamente.";
        return RedirectToPage(ListRouteValues);
    }

    private async Task<bool> TieneProductosActivosAsync(
        int supplierId,
        CancellationToken cancellationToken)
    {
        var productos = await productService.GetAllAsync(cancellationToken);
        return productos.Any(producto => producto.IdProveedor == supplierId);
    }

    private async Task LoadSuppliersAsync(CancellationToken cancellationToken)
    {
        PagedSuppliers = await supplierService.GetPagedAsync(Pagina, Tamano, Q, cancellationToken);
        Pagina = PagedSuppliers.Page;
        Tamano = PagedSuppliers.PageSize;
    }

    private void RemoveDeleteModelState()
    {
        ModelState.Remove(nameof(DeleteSupplierId));
        ModelState.Remove(nameof(Pagina));
        ModelState.Remove(nameof(Tamano));
        ModelState.Remove(nameof(Q));
    }

    private void LogModelStateErrors(string operation = "editar")
    {
        foreach (var entry in ModelState)
        {
            foreach (var error in entry.Value.Errors)
            {
                logger.LogWarning(
                    "Error de validación al {Operation} proveedor. Campo: {Field}. Error: {Error}",
                    operation,
                    entry.Key,
                    error.ErrorMessage);
            }
        }
    }
}