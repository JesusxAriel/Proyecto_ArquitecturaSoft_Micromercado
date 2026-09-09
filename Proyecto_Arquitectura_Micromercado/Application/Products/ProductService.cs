using Proyecto_Arquitectura_Micromercado.Domain.Products;
using System.Globalization;

namespace Proyecto_Arquitectura_Micromercado.Application.Products;

public sealed class ProductService(IProductRepository repository) : IProductService
{
    public Task<IReadOnlyList<ProductListItem>> GetAllAsync(CancellationToken cancellationToken = default) =>
        repository.GetAllAsync(cancellationToken);

    public Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        repository.GetByIdAsync(id, cancellationToken);

    public Task<IReadOnlyList<LookupOption>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        repository.GetCategoriesAsync(cancellationToken);

    public Task<IReadOnlyList<LookupOption>> GetSuppliersAsync(CancellationToken cancellationToken = default) =>
        repository.GetSuppliersAsync(cancellationToken);

    public Task<int> CreateAsync(Product product, CancellationToken cancellationToken = default)
    {
        Validate(product);
        return repository.CreateAsync(product, cancellationToken);
    }

    public Task<bool> UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        Validate(product);
        return repository.UpdateAsync(product, cancellationToken);
    }

    public Task<bool> SoftDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "El identificador debe ser positivo.");
        }

        return repository.SoftDeleteAsync(id, cancellationToken);
    }

    private static void Validate(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);
        NormalizeText(product);
        product.PrecioVenta = Math.Round(product.PrecioVenta, 2, MidpointRounding.AwayFromZero);
        product.PrecioCosto = Math.Round(product.PrecioCosto, 2, MidpointRounding.AwayFromZero);

        if (string.IsNullOrWhiteSpace(product.Nombre))
        {
            throw new ArgumentException("El nombre del producto es obligatorio.", nameof(product));
        }

        if (string.IsNullOrWhiteSpace(product.EmpaquePresentacion))
        {
            throw new ArgumentException("La presentación del producto es obligatoria.", nameof(product));
        }

        if (product.PrecioVenta < 0.10m ||
            decimal.Round(product.PrecioVenta * 10, 0) != product.PrecioVenta * 10 ||
            product.PrecioCosto <= 0 ||
            product.StockMinimo < 0 || product.IdCategoria <= 0 || product.IdProveedor <= 0)
        {
            throw new ArgumentException("Los datos del producto no son válidos.", nameof(product));
        }
    }

    private static void NormalizeText(Product product)
    {
        product.Nombre = ToTitleCase(product.Nombre);
        product.EmpaquePresentacion = ToTitleCase(product.EmpaquePresentacion);
    }

    private static string ToTitleCase(string text)
    {
        var singleSpacedText = string.Join(
            " ",
            text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));

        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(singleSpacedText.ToLower());
    }
}
