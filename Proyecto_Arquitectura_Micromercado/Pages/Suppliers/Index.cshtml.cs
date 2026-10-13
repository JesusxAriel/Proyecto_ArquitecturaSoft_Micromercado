using Microsoft.AspNetCore.Authorization;
using Proyecto_Arquitectura_Micromercado.Application.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Arquitectura_Micromercado.Application.Common;
using Proyecto_Arquitectura_Micromercado.Application.Suppliers;
using Proyecto_Arquitectura_Micromercado.Domain.Common;
using Proyecto_Arquitectura_Micromercado.Domain.Suppliers;
using Proyecto_Arquitectura_Micromercado.Pages.Shared;


namespace Proyecto_Arquitectura_Micromercado.Pages.Suppliers;

[Authorize(Roles = Roles.Administrador)]
public sealed class IndexModel(
    ISupplierService supplierService,
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

    public SupplierFormModel CreateSupplier { get; set; } = new();

    public SupplierFormModel EditSupplier { get; set; } = new();

    [BindProperty]
    public int DeleteSupplierId { get; set; }

    public bool ShowCreateModal { get; private set; }
    public bool ShowEditModal { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadSuppliersAsync(cancellationToken);
    }

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
        [FromForm] SupplierFormModel createSupplier,
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
            await supplierService.CreateAsync(CreateSupplier.ToSupplier(), cancellationToken);
            StatusMessage = "Proveedor creado correctamente.";
            return RedirectToPage(ListRouteValues);
        }
        catch (ArgumentException ex)
        {
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
        [FromForm] SupplierFormModel editSupplier,
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
            if (!await supplierService.UpdateAsync(EditSupplier.ToSupplier(), cancellationToken))
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

        try
        {
            if (!await supplierService.SoftDeleteAsync(DeleteSupplierId, cancellationToken))
            {
                return NotFound();
            }
        }
        catch (SupplierConProductosException ex)
        {
            ErrorMessage = ex.Message;
            return RedirectToPage(ListRouteValues);
        }

        StatusMessage = "Proveedor eliminado correctamente.";
        return RedirectToPage(ListRouteValues);
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