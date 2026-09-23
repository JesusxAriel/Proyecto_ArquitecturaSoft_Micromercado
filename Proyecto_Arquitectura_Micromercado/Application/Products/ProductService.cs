using Proyecto_Arquitectura_Micromercado.Domain.Products;
using System.Globalization;

namespace Proyecto_Arquitectura_Micromercado.Application.Products;

public sealed class ProductService(IProductRepository repository, IPriceHistoryRepository priceHistoryRepository) : IProductService
{
    // PRODUCTO.precioVenta / precioCosto son DECIMAL(10,2) en MySQL: 8 dígitos
    // enteros + 2 decimales. Un valor mayor desborda la columna.
    private const decimal MaxPrice = 99_999_999.99m;

    public Task<IReadOnlyList<ProductListItem>> GetAllAsync(CancellationToken cancellationToken = default) =>
        repository.GetAllAsync(cancellationToken);

    public Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        repository.GetByIdAsync(id, cancellationToken);

    public Task<IReadOnlyList<LookupOption>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        repository.GetCategoriesAsync(cancellationToken);

    public Task<IReadOnlyList<LookupOption>> GetSuppliersAsync(CancellationToken cancellationToken = default) =>
        repository.GetSuppliersAsync(cancellationToken);

    public Task<IReadOnlyList<ProductPriceHistory>> GetPriceHistoryAsync(
        CancellationToken cancellationToken = default) =>
        priceHistoryRepository.GetPriceHistoryAsync(cancellationToken);

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
            throw new ArgumentException("El nombre del producto es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(product.EmpaquePresentacion))
        {
            throw new ArgumentException("La presentación del producto es obligatoria.");
        }

        if (product.PrecioVenta < 0.10m ||
            decimal.Round(product.PrecioVenta * 10, 0) != product.PrecioVenta * 10)
        {
            throw new ArgumentException("El precio de venta debe ser al menos Bs. 0,10 y no puede tener más de un decimal significativo (ej. 5,20).");
        }

        if (product.PrecioVenta > MaxPrice)
        {
            throw new ArgumentException($"El precio de venta no puede superar Bs. {MaxPrice:N2}.");
        }

        if (product.PrecioCosto <= 0)
        {
            throw new ArgumentException("El precio de costo debe ser mayor a cero.");
        }

        if (product.PrecioCosto > MaxPrice)
        {
            throw new ArgumentException($"El precio de costo no puede superar Bs. {MaxPrice:N2}.");
        }

        if (product.StockMinimo < 0)
        {
            throw new ArgumentException("El stock mínimo no puede ser negativo.");
        }

        if (product.IdCategoria <= 0)
        {
            throw new ArgumentException("La categoría del producto es obligatoria.");
        }

        if (product.IdProveedor <= 0)
        {
            throw new ArgumentException("El proveedor del producto es obligatorio.");
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
