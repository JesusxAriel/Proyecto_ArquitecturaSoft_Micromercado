using MySql.Data.MySqlClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Arquitectura_Micromercado.Application.Suppliers;
using Proyecto_Arquitectura_Micromercado.Domain.Suppliers;

namespace Proyecto_Arquitectura_Micromercado.Pages.Suppliers;

public sealed class IndexModel(
    ISupplierService supplierService,
    ILogger<IndexModel> logger) : PageModel
{
    public IReadOnlyList<SupplierListItem> Suppliers { get; private set; } = [];
    public string? DatabaseWarning { get; private set; }

    [BindProperty]
    public Supplier CreateSupplier { get; set; } = new();

    [BindProperty]
    public Supplier EditSupplier { get; set; } = new();

    [BindProperty]
    public int DeleteSupplierId { get; set; }

    [BindProperty]
    public string DeleteSupplierName { get; set; } = string.Empty;

    [BindProperty]
    public string DeleteConfirmation { get; set; } = string.Empty;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadSuppliersAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostCreateAsync(CancellationToken cancellationToken)
    {
        RemoveDeleteModelState();

        if (!ModelState.IsValid)
        {
            await LoadSuppliersAsync(cancellationToken);
            return Page();
        }

        if (await supplierService.ExistsNombreEmpresaAsync(CreateSupplier.NombreEmpresa, 0, cancellationToken))
        {
            ModelState.AddModelError("CreateSupplier.NombreEmpresa", "Ya existe un proveedor con ese nombre.");
            await LoadSuppliersAsync(cancellationToken);
            return Page();
        }

        try
        {
            await supplierService.CreateAsync(CreateSupplier, cancellationToken);
            return RedirectToPage();
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadSuppliersAsync(cancellationToken);
            return Page();
        }
        catch (MySqlException)
        {
            DatabaseWarning = "No se pudo guardar el proveedor por un problema de conexión.";
            await LoadSuppliersAsync(cancellationToken);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostEditAsync(CancellationToken cancellationToken)
    {
        RemoveDeleteModelState();

        if (!ModelState.IsValid || EditSupplier.Id <= 0)
        {
            LogModelStateErrors();
            await LoadSuppliersAsync(cancellationToken);
            return Page();
        }

        if (await supplierService.ExistsNombreEmpresaAsync(EditSupplier.NombreEmpresa, EditSupplier.Id, cancellationToken))
        {
            ModelState.AddModelError("EditSupplier.NombreEmpresa", "Ya existe un proveedor con ese nombre.");
            await LoadSuppliersAsync(cancellationToken);
            return Page();
        }

        try
        {
            if (!await supplierService.UpdateAsync(EditSupplier, cancellationToken))
            {
                return NotFound();
            }

            return RedirectToPage();
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadSuppliersAsync(cancellationToken);
            return Page();
        }
        catch (MySqlException)
        {
            DatabaseWarning = "No se pudo actualizar el proveedor por un problema de conexión.";
            await LoadSuppliersAsync(cancellationToken);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(CancellationToken cancellationToken)
    {
        if (DeleteSupplierId <= 0 ||
            !string.Equals(DeleteSupplierName, DeleteConfirmation, StringComparison.Ordinal))
        {
            return BadRequest("La confirmación del nombre no coincide exactamente.");
        }

        if (!await supplierService.SoftDeleteAsync(DeleteSupplierId, cancellationToken))
        {
            return NotFound();
        }

        return RedirectToPage();
    }

    private async Task LoadSuppliersAsync(CancellationToken cancellationToken)
    {
        Suppliers = (await supplierService.GetAllAsync(cancellationToken))
            .OrderBy(supplier => supplier.NombreEmpresa)
            .ToList();
    }

    private void RemoveDeleteModelState()
    {
        ModelState.Remove(nameof(DeleteSupplierName));
        ModelState.Remove(nameof(DeleteConfirmation));
        ModelState.Remove(nameof(DeleteSupplierId));
    }

    private void LogModelStateErrors()
    {
        foreach (var entry in ModelState)
        {
            foreach (var error in entry.Value.Errors)
            {
                logger.LogWarning(
                    "Error de validación al editar proveedor. Campo: {Field}. Error: {Error}",
                    entry.Key,
                    error.ErrorMessage);
            }
        }
    }
}