using Proyecto_Arquitectura_Micromercado.Domain.Common;
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

    public async Task<PagedResult<ProductListItem>> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default)
    {
        pageSize = PageSizes.Normalize(pageSize);
        page = Math.Max(1, page);
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        var result = await repository.GetPagedAsync(page, pageSize, search, cancellationToken);

        // Si la página pedida quedó fuera de rango (p. ej. se eliminaron registros), se muestra la última.
        if (result.Items.Count == 0 && result.TotalCount > 0 && page > result.TotalPages)
        {
            result = await repository.GetPagedAsync(result.TotalPages, pageSize, search, cancellationToken);
        }

        return result;
    }

    public Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        repository.GetByIdAsync(id, cancellationToken);

    public Task<IReadOnlyList<LookupOption>> GetPackagingsAsync(CancellationToken cancellationToken = default) =>
        repository.GetPackagingsAsync(cancellationToken);

    public Task<IReadOnlyList<LookupOption>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        repository.GetCategoriesAsync(cancellationToken);

    public Task<IReadOnlyList<LookupOption>> GetSuppliersAsync(CancellationToken cancellationToken = default) =>
        repository.GetSuppliersAsync(cancellationToken);

    public Task<IReadOnlyList<ProductPriceHistory>> GetPriceHistoryAsync(
        CancellationToken cancellationToken = default) =>
        priceHistoryRepository.GetPriceHistoryAsync(cancellationToken);

    public async Task<int> CreateAsync(Product product, CancellationToken cancellationToken = default)
    {
        Validate(product);
        await EnsureNotDuplicatedAsync(product, cancellationToken);
        await EnsureReferencesExistAsync(product, cancellationToken);

        return await repository.CreateAsync(product, cancellationToken);
    }

    public async Task<bool> UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        Validate(product);
        await EnsureNotDuplicatedAsync(product, cancellationToken);
        await EnsureReferencesExistAsync(product, cancellationToken);

        return await repository.UpdateAsync(product, cancellationToken);
    }

    // Al crear Id vale 0, así que no excluye a nadie; al editar excluye el propio registro.
    private async Task EnsureNotDuplicatedAsync(Product product, CancellationToken cancellationToken)
    {
        if (await repository.ExistsNombreEmpaqueAsync(
                product.Nombre,
                product.IdEmpaque,
                product.Id,
                cancellationToken))
        {
            throw new ArgumentException(ProductValidation.DuplicateMessage);
        }
    }

    private async Task EnsureReferencesExistAsync(Product product, CancellationToken cancellationToken)
    {
        var packagings = await repository.GetPackagingsAsync(cancellationToken);
        if (!packagings.Any(p => p.Id == product.IdEmpaque))
        {
            throw new ArgumentException("El empaque seleccionado no existe.");
        }

        var categories = await repository.GetCategoriesAsync(cancellationToken);
        if (!categories.Any(c => c.Id == product.IdCategoria))
        {
            throw new ArgumentException("La categoría seleccionada no existe.");
        }

        var suppliers = await repository.GetSuppliersAsync(cancellationToken);
        if (!suppliers.Any(s => s.Id == product.IdProveedor))
        {
            throw new ArgumentException("El proveedor seleccionado no existe.");
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

        if (product.IdEmpaque <= 0)
        {
            throw new ArgumentException("El empaque del producto es obligatorio.");
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

        if (product.PrecioVenta < product.PrecioCosto)
        {
            throw new ArgumentException(ProductPriceValidation.SaleBelowCostMessage);
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
    }

    private static string ToTitleCase(string text)
    {
        var singleSpacedText = string.Join(
            " ",
            text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));

        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(singleSpacedText.ToLower());
    }
}
