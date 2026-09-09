using Proyecto_Arquitectura_Micromercado.Domain.Products;

namespace Proyecto_Arquitectura_Micromercado.Application.Products;

public interface IProductRepository
{
    Task<IReadOnlyList<ProductListItem>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LookupOption>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LookupOption>> GetSuppliersAsync(CancellationToken cancellationToken = default);
    Task<int> CreateAsync(Product product, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Product product, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(int id, CancellationToken cancellationToken = default);
}
