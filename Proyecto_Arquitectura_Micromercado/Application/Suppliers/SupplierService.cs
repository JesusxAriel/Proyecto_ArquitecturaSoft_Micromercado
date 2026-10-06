using Proyecto_Arquitectura_Micromercado.Application.Common;
using Proyecto_Arquitectura_Micromercado.Domain.Common;
using Proyecto_Arquitectura_Micromercado.Domain.Suppliers;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Proyecto_Arquitectura_Micromercado.Application.Suppliers;

public sealed class SupplierService(ISupplierRepository repository) : ISupplierService
{
    // Tras normalizar (sin +591), el teléfono guardado son 8 dígitos que inician con 6 o 7.
    private static readonly Regex TelefonoBolivia = new(@"^[67]\d{7}$", RegexOptions.CultureInvariant);

    public Task<IReadOnlyList<SupplierListItem>> GetAllAsync(CancellationToken cancellationToken = default) =>
        repository.GetAllAsync(cancellationToken);

    public async Task<PagedResult<SupplierListItem>> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default)
    {
        search = PagedQuery.NormalizeSearch(search);

        return await PagedQuery.ExecuteAsync(
            page,
            pageSize,
            (currentPage, currentSize) =>
                repository.GetPagedAsync(currentPage, currentSize, search, cancellationToken));
    }

    public Task<Supplier?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        repository.GetByIdAsync(id, cancellationToken);

    public Task<bool> ExistsNombreEmpresaAsync(
        string nombreEmpresa,
        int idExcluido,
        CancellationToken cancellationToken = default) =>
        repository.ExistsNombreEmpresaAsync(ToTitleCase(nombreEmpresa), idExcluido, cancellationToken);

    public async Task<int> CreateAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        Validate(supplier);
        await EnsureNameIsUniqueAsync(supplier, cancellationToken);
        return await repository.CreateAsync(supplier, cancellationToken);
    }

    public async Task<bool> UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        Validate(supplier);
        await EnsureNameIsUniqueAsync(supplier, cancellationToken);
        return await repository.UpdateAsync(supplier, cancellationToken);
    }

    // Al crear Id vale 0, así que no excluye a nadie; al editar excluye el propio registro.
    private async Task EnsureNameIsUniqueAsync(Supplier supplier, CancellationToken cancellationToken)
    {
        if (await repository.ExistsNombreEmpresaAsync(supplier.NombreEmpresa, supplier.Id, cancellationToken))
        {
            throw new DuplicateSupplierException();
        }
    }

    public Task<bool> SoftDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "El identificador debe ser positivo.");
        }

        return repository.SoftDeleteAsync(id, cancellationToken);
    }

    private static void Validate(Supplier supplier)
    {
        ArgumentNullException.ThrowIfNull(supplier);
        NormalizeText(supplier);

        if (string.IsNullOrWhiteSpace(supplier.NombreEmpresa))
        {
            throw new ArgumentException("El nombre de la empresa es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(supplier.NumeroEmpresa))
        {
            throw new ArgumentException("El teléfono de contacto es obligatorio.");
        }

        if (!TelefonoBolivia.IsMatch(supplier.NumeroEmpresa))
        {
            throw new ArgumentException(SupplierValidation.TelefonoMessage);
        }

        if (supplier.EsAutogestionado && string.IsNullOrWhiteSpace(supplier.CorreoReferencia))
        {
            throw new ArgumentException("Un proveedor autogestionado requiere correo.");
        }
    }

    private static void NormalizeText(Supplier supplier)
    {
        supplier.NombreEmpresa = ToTitleCase(supplier.NombreEmpresa);
        supplier.NumeroEmpresa = SupplierValidation.NormalizeTelefono(supplier.NumeroEmpresa);
        supplier.CorreoReferencia = string.IsNullOrWhiteSpace(supplier.CorreoReferencia)
            ? null
            : supplier.CorreoReferencia.Trim().ToLowerInvariant();
    }

    private static string ToTitleCase(string text)
    {
        var singleSpacedText = string.Join(
            " ",
            text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));

        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(singleSpacedText.ToLower());
    }
}