using Proyecto_Arquitectura_Micromercado.Domain.Suppliers;
using System.Globalization;

namespace Proyecto_Arquitectura_Micromercado.Application.Suppliers;

public sealed class SupplierService(ISupplierRepository repository) : ISupplierService
{
    public Task<IReadOnlyList<SupplierListItem>> GetAllAsync(CancellationToken cancellationToken = default) =>
        repository.GetAllAsync(cancellationToken);

    public Task<Supplier?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        repository.GetByIdAsync(id, cancellationToken);

    public Task<bool> ExistsNombreEmpresaAsync(
        string nombreEmpresa,
        int idExcluido,
        CancellationToken cancellationToken = default) =>
        repository.ExistsNombreEmpresaAsync(ToTitleCase(nombreEmpresa), idExcluido, cancellationToken);

    public Task<int> CreateAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        Validate(supplier);
        return repository.CreateAsync(supplier, cancellationToken);
    }

    public Task<bool> UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        Validate(supplier);
        return repository.UpdateAsync(supplier, cancellationToken);
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
            throw new ArgumentException("El nombre de la empresa es obligatorio.", nameof(supplier));
        }

        if (string.IsNullOrWhiteSpace(supplier.NumeroEmpresa))
        {
            throw new ArgumentException("El teléfono de contacto es obligatorio.", nameof(supplier));
        }

        if (supplier.EsAutogestionado && string.IsNullOrWhiteSpace(supplier.CorreoReferencia))
        {
            throw new ArgumentException("Un proveedor autogestionado requiere correo.", nameof(supplier));
        }
    }

    private static void NormalizeText(Supplier supplier)
    {
        supplier.NombreEmpresa = ToTitleCase(supplier.NombreEmpresa);
        supplier.NumeroEmpresa = supplier.NumeroEmpresa.Trim();
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