using Proyecto_Arquitectura_Micromercado.Domain.Products;

namespace Proyecto_Arquitectura_Micromercado.Application.Products;

public interface IConCatalogo
{
    Task<IReadOnlyList<LookupOption>> GetCategoriesAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LookupOption>> GetSuppliersAsync(
        CancellationToken cancellationToken = default);
}
