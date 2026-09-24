using MySql.Data.MySqlClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Arquitectura_Micromercado.Application.Suppliers;
using Proyecto_Arquitectura_Micromercado.Domain.Suppliers;

using Proyecto_Arquitectura_Micromercado.Application.Products;

namespace Proyecto_Arquitectura_Micromercado.Pages.Suppliers;

public sealed class IndexModel(
    ISupplierService supplierService,
    IProductService productService,
    ILogger<IndexModel> logger) : PageModel
{
    public IReadOnlyList<SupplierListItem> Suppliers { get; private set; } = [];
    public string? DatabaseWarning { get; private set; }
    [TempData]
    public string? ErrorMessage { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public Supplier CreateSupplier { get; set; } = new();

    public Supplier EditSupplier { get; set; } = new();

    [BindProperty]
    public int DeleteSupplierId { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadSuppliersAsync(cancellationToken);
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
            StatusMessage = "Proveedor creado correctamente.";
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

    public async Task<IActionResult> OnPostEditAsync(
        [FromForm] Supplier editSupplier,
        CancellationToken cancellationToken)
    {
        EditSupplier = editSupplier;
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

            StatusMessage = "Proveedor actualizado correctamente.";
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
        if (DeleteSupplierId <= 0)
        {
            return BadRequest("El proveedor no es válido.");
        }

        if (await TieneProductosActivosAsync(DeleteSupplierId, cancellationToken))
        {
            ErrorMessage = "No se puede eliminar el proveedor porque tiene productos asociados.";
            return RedirectToPage();
        }

        if (!await supplierService.SoftDeleteAsync(DeleteSupplierId, cancellationToken))
        {
            return NotFound();
        }

        StatusMessage = "Proveedor eliminado correctamente.";
        return RedirectToPage();
    }

    /// <summary>
    /// Verifica si el proveedor abastece productos activos. La baja de proveedores
    /// es lógica, por lo que MySQL nunca rechaza la operación aunque la clave
    /// foránea sea ON DELETE RESTRICT: la regla debe comprobarse antes de dar de baja.
    /// </summary>
    private async Task<bool> TieneProductosActivosAsync(
        int supplierId,
        CancellationToken cancellationToken)
    {
        var productos = await productService.GetAllAsync(cancellationToken);
        return productos.Any(producto => producto.IdProveedor == supplierId);
    }

    private async Task LoadSuppliersAsync(CancellationToken cancellationToken)
    {
        Suppliers = (await supplierService.GetAllAsync(cancellationToken))
            .OrderBy(supplier => supplier.NombreEmpresa)
            .ToList();
    }

    private void RemoveDeleteModelState()
    {
        ModelState.Remove(nameof(DeleteSupplierId));
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