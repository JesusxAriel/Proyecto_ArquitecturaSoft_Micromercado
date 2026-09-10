# Diagnóstico del módulo de Proveedores

Este documento contiene el código fuente actual relacionado con el binding, validación y formularios del módulo de Proveedores. El objetivo es comprobar que los nombres enviados por los formularios coincidan con las propiedades que recibe `IndexModel`.

## 1. Backend - PageModel

Archivo: `Proyecto_Arquitectura_Micromercado/Pages/Suppliers/Index.cshtml.cs`

```csharp
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
    [TempData]
    public string? StatusMessage { get; set; }

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
        RemoveModelStateForSupplier(nameof(EditSupplier));

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

    public async Task<IActionResult> OnPostEditAsync(CancellationToken cancellationToken)
    {
        RemoveDeleteModelState();
        RemoveModelStateForSupplier(nameof(CreateSupplier));

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

    private void RemoveModelStateForSupplier(string propertyName)
    {
        var keysToRemove = ModelState.Keys
            .Where(key =>
                key.Equals(propertyName, StringComparison.OrdinalIgnoreCase) ||
                key.StartsWith($"{propertyName}.", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var key in keysToRemove)
        {
            ModelState.Remove(key);
        }
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
```

### Propiedades recibidas por cada formulario

El formulario de Crear debe enviar:

```text
CreateSupplier.NombreEmpresa
CreateSupplier.NumeroEmpresa
CreateSupplier.CorreoReferencia
CreateSupplier.EsAutogestionado
```

El formulario de Editar debe enviar:

```text
EditSupplier.Id
EditSupplier.NombreEmpresa
EditSupplier.NumeroEmpresa
EditSupplier.CorreoReferencia
EditSupplier.EsAutogestionado
```

## 2. Domain / DTOs y validaciones

No existe un archivo `SupplierDtos.cs` separado en el proyecto actual. Las clases que reciben y validan los datos se encuentran en `Proyecto_Arquitectura_Micromercado/Domain/Suppliers/Supplier.cs` y `Proyecto_Arquitectura_Micromercado/Domain/Suppliers/SupplierDtos.cs`.

### Archivo: `Domain/Suppliers/Supplier.cs`

```csharp
﻿using System.ComponentModel.DataAnnotations;

namespace Proyecto_Arquitectura_Micromercado.Domain.Suppliers;

public sealed class Supplier : IValidatableObject
{
    public int Id { get; set; }

    [Display(Name = "Nombre de la empresa")]
    [Required(ErrorMessage = "Campo obligatorio.")]
    [StringLength(150)]
    [SupplierText]
    public string NombreEmpresa { get; set; } = string.Empty;

    [Display(Name = "Teléfono de contacto")]
    [Required(ErrorMessage = "Campo obligatorio.")]
    [Telefono(ErrorMessage = SupplierValidation.TelefonoMessage)]
    public string NumeroEmpresa { get; set; } = string.Empty;

    [Display(Name = "Correo de referencia")]
    [EmailAddress(ErrorMessage = SupplierValidation.CorreoMessage)]
    [StringLength(150)]
    public string? CorreoReferencia { get; set; }

    [Display(Name = "Proveedor autogestionado")]
    public bool EsAutogestionado { get; set; }

    public bool EstaActivo { get; set; } = true;

    /// <summary>
    /// Regla que involucra a más de un campo, por lo que no puede resolverse
    /// con un atributo sobre una sola propiedad.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EsAutogestionado && string.IsNullOrWhiteSpace(CorreoReferencia))
        {
            yield return new ValidationResult(
                SupplierValidation.CorreoRequeridoParaAutogestionadoMessage,
                [nameof(CorreoReferencia)]);
        }
    }
}

public sealed class SupplierListItem
{
    public int Id { get; init; }
    public string NombreEmpresa { get; init; } = string.Empty;
    public string NumeroEmpresa { get; init; } = string.Empty;
    public string CorreoReferencia { get; init; } = string.Empty;
    public bool EsAutogestionado { get; init; }
}
```

### Archivo: `Domain/Suppliers/SupplierDtos.cs`

```csharp
﻿using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Proyecto_Arquitectura_Micromercado.Domain.Suppliers;

public static class SupplierValidation
{
    public const string TelefonoPattern = @"^\d{6,20}$";

    public const string TelefonoMessage =
        "Solo dígitos, entre 6 y 20. Ej: 44112233";
    public const string CorreoMessage =
        "Formato inválido. Ej: ventas@empresa.com.bo";
    public const string NombreDuplicadoMessage =
        "Ya existe un proveedor registrado con ese nombre de empresa.";
    public const string CorreoRequeridoParaAutogestionadoMessage =
        "Un proveedor autogestionado necesita un correo para coordinar sus reposiciones.";
}

public sealed class SupplierTextAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is not string text || string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        return !text.Any(char.IsControl);
    }

    public override string FormatErrorMessage(string name) =>
        $"El campo {name} debe tener palabras separadas por un solo espacio.";
}

public sealed class TelefonoAttribute : ValidationAttribute
{
    private static readonly Regex Pattern =
        new(SupplierValidation.TelefonoPattern, RegexOptions.CultureInvariant);

    public override bool IsValid(object? value)
    {
        if (value is not string text || string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        return Pattern.IsMatch(text.Trim());
    }

    public override string FormatErrorMessage(string name) =>
        SupplierValidation.TelefonoMessage;
}
```

## 3. Frontend - Vista Razor de Proveedores

Archivo: `Proyecto_Arquitectura_Micromercado/Pages/Suppliers/Index.cshtml`

```cshtml
@page
@model Proyecto_Arquitectura_Micromercado.Pages.Suppliers.IndexModel

@{
    ViewData["Title"] = "Proveedores";
}

<div class="d-flex flex-wrap justify-content-between align-items-center gap-3 mb-4">
    <h1 class="page-heading mb-0">Proveedores</h1>
    <button type="button" class="btn btn-warning fw-semibold px-3"
            data-bs-toggle="modal" data-bs-target="#createSupplierModal">
        <i class="bi bi-plus-lg me-1"></i>Añadir
    </button>
</div>

@if (!string.IsNullOrWhiteSpace(Model.StatusMessage))
{
    <div class="alert alert-success shadow-sm" role="alert">@Model.StatusMessage</div>
}

@if (!string.IsNullOrWhiteSpace(Model.DatabaseWarning))
{
    <div class="alert alert-warning shadow-sm" role="alert">@Model.DatabaseWarning</div>
}

<div class="mb-3">
    <label for="supplierSearch" class="visually-hidden">Buscar proveedores</label>
    <div class="input-group">
        <span class="input-group-text"><i class="bi bi-search" aria-hidden="true"></i></span>
        <input id="supplierSearch" class="form-control" type="search"
               placeholder="Buscar por empresa, teléfono o correo..." autocomplete="off" />
    </div>
</div>

<div class="table-responsive shadow-sm rounded-3">
    <table id="suppliersTable" class="table table-hover align-middle mb-0">
        <thead>
            <tr>
                <th>Empresa</th>
                <th>Teléfono</th>
                <th>Correo de referencia</th>
                <th>Modalidad</th>
                <th class="text-end pe-3">ACCIONES</th>
            </tr>
        </thead>
        <tbody>
        @foreach (var supplier in Model.Suppliers)
        {
            <tr>
                <td>@supplier.NombreEmpresa</td>
                <td>@supplier.NumeroEmpresa</td>
                <td>@(string.IsNullOrEmpty(supplier.CorreoReferencia) ? "—" : supplier.CorreoReferencia)</td>
                <td>
                    @if (supplier.EsAutogestionado)
                    {
                        <span class="badge badge-ok">Autogestionado</span>
                    }
                    else
                    {
                        <span class="badge text-bg-secondary">Compra directa</span>
                    }
                </td>
                <td class="text-end pe-3">
                    <div class="d-flex justify-content-end align-items-center gap-1">
                        <button type="button" class="btn btn-sm btn-outline-warning edit-supplier-button"
                                data-bs-toggle="modal" data-bs-target="#editSupplierModal"
                                data-id="@supplier.Id" data-name="@supplier.NombreEmpresa"
                                data-phone="@supplier.NumeroEmpresa" data-email="@supplier.CorreoReferencia"
                                data-self-managed="@supplier.EsAutogestionado"
                                title="Editar" aria-label="Editar @supplier.NombreEmpresa">
                            <i class="bi bi-pencil-square"></i>
                        </button>
                        <button type="button" class="btn btn-sm btn-outline-danger delete-supplier-button"
                                data-bs-toggle="modal" data-bs-target="#deleteSupplierModal"
                                data-id="@supplier.Id" data-name="@supplier.NombreEmpresa"
                                title="Eliminar" aria-label="Eliminar @supplier.NombreEmpresa">
                            <i class="bi bi-trash"></i>
                        </button>
                    </div>
                </td>
            </tr>
        }
        </tbody>
    </table>
</div>

@if (Model.Suppliers.Count == 0)
{
    <p class="text-muted">No hay proveedores activos registrados.</p>
}

<div class="modal fade" id="createSupplierModal" tabindex="-1"
     aria-labelledby="createSupplierLabel" aria-hidden="true">
    <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content">
            <form method="post" asp-page-handler="Create" class="modal-form">
                @Html.AntiForgeryToken()
                <div class="modal-header">
                    <h2 class="modal-title h5" id="createSupplierLabel">Añadir proveedor</h2>
                    <button type="button" class="btn-close" data-bs-dismiss="modal"
                            aria-label="Cerrar"></button>
                </div>
                <div class="modal-body">
                    <div asp-validation-summary="ModelOnly" class="text-danger"></div>
                    <div class="mb-3">
                        <label for="CreateSupplier_NombreEmpresa" class="form-label">Nombre de la empresa</label>
                        <input asp-for="CreateSupplier.NombreEmpresa" id="CreateSupplier_NombreEmpresa" class="form-control"
                               required data-sanitize="text" data-validate-input
                               pattern="^[a-zA-Z0-9áéíóúÁÉÍÓÚñÑ\s\.\,\-]+$" />
                        <span asp-validation-for="CreateSupplier.NombreEmpresa" class="text-danger"></span>
                        <div class="input-error text-danger small d-none"></div>
                    </div>
                    <div class="mb-3">
                        <label for="CreateSupplier_NumeroEmpresa" class="form-label">Teléfono de contacto</label>
                        <input asp-for="CreateSupplier.NumeroEmpresa" id="CreateSupplier_NumeroEmpresa" class="form-control"
                               required data-validate-input pattern="^\d{7,15}$"
                               data-validation-message="El teléfono debe contener entre 7 y 15 dígitos numéricos."
                               inputmode="numeric" />
                        <span asp-validation-for="CreateSupplier.NumeroEmpresa" class="text-danger"></span>
                        <div class="input-error text-danger small d-none"></div>
                    </div>
                    <div class="mb-3">
                        <label for="CreateSupplier_CorreoReferencia" class="form-label">Correo de referencia</label>
                        <input asp-for="CreateSupplier.CorreoReferencia" id="CreateSupplier_CorreoReferencia" class="form-control"
                               type="email" data-validate-input
                               data-validation-message="Ingrese un correo electrónico válido con dominio (ejemplo@dominio.com)." />
                        <span asp-validation-for="CreateSupplier.CorreoReferencia" class="text-danger"></span>
                        <div class="input-error text-danger small d-none"></div>
                    </div>
                    <div class="form-check">
                        <input asp-for="CreateSupplier.EsAutogestionado" id="CreateSupplier_EsAutogestionado" class="form-check-input" />
                        <label for="CreateSupplier_EsAutogestionado" class="form-check-label">
                            Proveedor autogestionado
                        </label>
                    </div>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary"
                            data-bs-dismiss="modal">Cancelar</button>
                    <button type="submit" class="btn btn-primary modal-save-button">
                        Guardar
                    </button>
                </div>
            </form>
        </div>
    </div>
</div>

<div class="modal fade" id="editSupplierModal" tabindex="-1"
     aria-labelledby="editSupplierLabel" aria-hidden="true">
    <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content">
            <form method="post" asp-page-handler="Edit" class="modal-form">
                @Html.AntiForgeryToken()
                <div class="modal-header">
                    <h2 class="modal-title h5" id="editSupplierLabel">Editar proveedor</h2>
                    <button type="button" class="btn-close" data-bs-dismiss="modal"
                            aria-label="Cerrar"></button>
                </div>
                <div class="modal-body">
                    <input type="hidden" asp-for="EditSupplier.Id" id="EditSupplier_Id" />
                    <div asp-validation-summary="ModelOnly" class="text-danger"></div>
                    <div class="mb-3">
                        <label for="EditSupplier_NombreEmpresa" class="form-label">Nombre de la empresa</label>
                        <input asp-for="EditSupplier.NombreEmpresa" id="EditSupplier_NombreEmpresa"
                               class="form-control" required data-sanitize="text"
                               data-validate-input
                               pattern="^[a-zA-Z0-9áéíóúÁÉÍÓÚñÑ\s\.\,\-]+$" />
                        <span asp-validation-for="EditSupplier.NombreEmpresa" class="text-danger"></span>
                        <div class="input-error text-danger small d-none"></div>
                    </div>
                    <div class="mb-3">
                        <label for="EditSupplier_NumeroEmpresa" class="form-label">Teléfono de contacto</label>
                        <input asp-for="EditSupplier.NumeroEmpresa" id="EditSupplier_NumeroEmpresa"
                               class="form-control" required data-validate-input
                               pattern="^\d{7,15}$"
                               data-validation-message="El teléfono debe contener entre 7 y 15 dígitos numéricos."
                               inputmode="numeric" />
                        <span asp-validation-for="EditSupplier.NumeroEmpresa" class="text-danger"></span>
                        <div class="input-error text-danger small d-none"></div>
                    </div>
                    <div class="mb-3">
                        <label for="EditSupplier_CorreoReferencia" class="form-label">Correo de referencia</label>
                        <input asp-for="EditSupplier.CorreoReferencia" id="EditSupplier_CorreoReferencia"
                               class="form-control" type="email" data-validate-input
                               data-validation-message="Ingrese un correo electrónico válido con dominio (ejemplo@dominio.com)." />
                        <span asp-validation-for="EditSupplier.CorreoReferencia" class="text-danger"></span>
                        <div class="input-error text-danger small d-none"></div>
                    </div>
                    <div class="form-check">
                        <input asp-for="EditSupplier.EsAutogestionado"
                               id="EditSupplier_EsAutogestionado" class="form-check-input" />
                        <label for="EditSupplier_EsAutogestionado" class="form-check-label">
                            Proveedor autogestionado
                        </label>
                    </div>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary"
                            data-bs-dismiss="modal">Cancelar</button>
                    <button type="submit" class="btn btn-primary modal-save-button">
                        Guardar cambios
                    </button>
                </div>
            </form>
        </div>
    </div>
</div>

<div class="modal fade" id="deleteSupplierModal" tabindex="-1"
     aria-labelledby="deleteSupplierLabel" aria-hidden="true">
    <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content">
            <form method="post" asp-page-handler="Delete">
                @Html.AntiForgeryToken()
                <div class="modal-header">
                    <h2 class="modal-title h5" id="deleteSupplierLabel">Eliminar proveedor</h2>
                    <button type="button" class="btn-close" data-bs-dismiss="modal"
                            aria-label="Cerrar"></button>
                </div>
                <div class="modal-body">
                    <input type="hidden" asp-for="DeleteSupplierId" id="deleteSupplierId" />
                    <input type="hidden" asp-for="DeleteSupplierName" id="deleteSupplierName" />
                    <p>Escribe exactamente <strong id="deleteSupplierNameLabel"></strong> para confirmar.</p>
                    <label for="deleteSupplierConfirmation" class="form-label">Nombre del proveedor</label>
                    <input asp-for="DeleteConfirmation" id="deleteSupplierConfirmation"
                           class="form-control" autocomplete="off" required />
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary"
                            data-bs-dismiss="modal">Cancelar</button>
                    <button type="submit" id="confirmSupplierDelete"
                            class="btn btn-danger" disabled>
                        <i class="bi bi-trash"></i> Eliminar
                    </button>
                </div>
            </form>
        </div>
    </div>
</div>

@section Scripts {
    <script>
        $('.edit-supplier-button').on('click', function () {
            const button = $(this);
            $('#EditSupplier_Id').val(button.attr('data-id'));
            $('#EditSupplier_NombreEmpresa').val(button.attr('data-name'));
            $('#EditSupplier_NumeroEmpresa').val(button.attr('data-phone'));
            $('#EditSupplier_CorreoReferencia').val(button.attr('data-email') || '');
            $('#EditSupplier_EsAutogestionado').prop(
                'checked',
                button.attr('data-self-managed') === 'True');
            $('#editSupplierModal [data-validate-input]').trigger('input');
        });

        $('.delete-supplier-button').on('click', function () {
            const button = $(this);
            $('#deleteSupplierId').val(button.attr('data-id'));
            $('#deleteSupplierName').val(button.attr('data-name'));
            $('#deleteSupplierNameLabel').text(button.attr('data-name'));
            $('#deleteSupplierConfirmation').val('');
            $('#confirmSupplierDelete').prop('disabled', true);
        });

        $('#deleteSupplierConfirmation').on('input', function () {
            $('#confirmSupplierDelete').prop(
                'disabled',
                this.value !== $('#deleteSupplierName').val());
        });

        $('#supplierSearch').on('input', function () {
            const query = $(this).val().toString().toLocaleLowerCase();
            $('#suppliersTable tbody tr').each(function () {
                $(this).toggle($(this).text().toLocaleLowerCase().includes(query));
            });
        });
    </script>
}
```

### Archivo: `Pages/Suppliers/_SupplierForm.cshtml`

```cshtml
﻿@model Proyecto_Arquitectura_Micromercado.Pages.Suppliers.SupplierFormModel

<form method="post">
    @Html.AntiForgeryToken()
    <input type="hidden" asp-for="Supplier.Id" />
    <div asp-validation-summary="ModelOnly" class="text-danger"></div>
    <div class="row g-3">
        <div class="col-md-8">
            <label asp-for="Supplier.NombreEmpresa" class="form-label"></label>
            <input asp-for="Supplier.NombreEmpresa" class="form-control" />
            <span asp-validation-for="Supplier.NombreEmpresa" class="text-danger"></span>
        </div>
        <div class="col-md-4">
            <label asp-for="Supplier.NumeroEmpresa" class="form-label"></label>
            <input asp-for="Supplier.NumeroEmpresa" class="form-control" type="text" inputmode="numeric" pattern="^\d{7,15}$" placeholder="44112233" />
            <span asp-validation-for="Supplier.NumeroEmpresa" class="text-danger"></span>
        </div>
        <div class="col-md-8">
            <label asp-for="Supplier.CorreoReferencia" class="form-label"></label>
            <input asp-for="Supplier.CorreoReferencia" class="form-control" type="email" pattern="^[^@@\s]+@@[^@@\s]+\.[^@@\s]+$" placeholder="ventas@empresa.com.bo" />
            <span asp-validation-for="Supplier.CorreoReferencia" class="text-danger"></span>
        </div>
        <div class="col-md-12">
            <div class="form-check">
                <input asp-for="Supplier.EsAutogestionado" class="form-check-input" />
                <label asp-for="Supplier.EsAutogestionado" class="form-check-label">
                    Proveedor autogestionado (deja mercadería en consigna y repone por su cuenta)
                </label>
            </div>
        </div>
    </div>
    <div class="mt-4">
        <button type="submit" class="btn btn-primary">Guardar</button>
        <a asp-page="/Suppliers/Index" class="btn btn-secondary">Cancelar</a>
    </div>
</form>
```

## 4. JavaScript de Proveedores

El siguiente bloque es el script completo incluido en `Pages/Suppliers/Index.cshtml`. La parte de edición captura los atributos `data-*` de la fila y los asigna a los campos cuyos `id` corresponden al binding `EditSupplier.*`.

```javascript
$('.edit-supplier-button').on('click', function () {
    const button = $(this);
    $('#EditSupplier_Id').val(button.attr('data-id'));
    $('#EditSupplier_NombreEmpresa').val(button.attr('data-name'));
    $('#EditSupplier_NumeroEmpresa').val(button.attr('data-phone'));
    $('#EditSupplier_CorreoReferencia').val(button.attr('data-email') || '');
    $('#EditSupplier_EsAutogestionado').prop(
        'checked',
        button.attr('data-self-managed') === 'True');
    $('#editSupplierModal [data-validate-input]').trigger('input');
});

$('.delete-supplier-button').on('click', function () {
    const button = $(this);
    $('#deleteSupplierId').val(button.attr('data-id'));
    $('#deleteSupplierName').val(button.attr('data-name'));
    $('#deleteSupplierNameLabel').text(button.attr('data-name'));
    $('#deleteSupplierConfirmation').val('');
    $('#confirmSupplierDelete').prop('disabled', true);
});

$('#deleteSupplierConfirmation').on('input', function () {
    $('#confirmSupplierDelete').prop(
        'disabled',
        this.value !== $('#deleteSupplierName').val());
});

$('#supplierSearch').on('input', function () {
    const query = $(this).val().toString().toLocaleLowerCase();
    $('#suppliersTable tbody tr').each(function () {
        $(this).toggle($(this).text().toLocaleLowerCase().includes(query));
    });
});
```

## Relación de nombres del binding

`asp-for="CreateSupplier.NombreEmpresa"` genera un input con:

```html
name="CreateSupplier.NombreEmpresa"
id="CreateSupplier_NombreEmpresa"
```

`asp-for="EditSupplier.NombreEmpresa"` genera un input con:

```html
name="EditSupplier.NombreEmpresa"
id="EditSupplier_NombreEmpresa"
```

Lo mismo se aplica a `NumeroEmpresa`, `CorreoReferencia` y `EsAutogestionado`. Si el navegador envía solamente `Name`, `Phone` u otro nombre distinto, el binder no poblará `CreateSupplier` o `EditSupplier`, y las anotaciones `[Required]` de `NombreEmpresa` y `NumeroEmpresa` producirán el mensaje `Campo obligatorio.`.
