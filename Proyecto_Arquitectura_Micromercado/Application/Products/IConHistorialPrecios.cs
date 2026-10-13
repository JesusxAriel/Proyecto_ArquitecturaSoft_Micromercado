using Proyecto_Arquitectura_Micromercado.Domain.Products;

namespace Proyecto_Arquitectura_Micromercado.Application.Products;

public interface IConHistorialPrecios
{
    Task<IReadOnlyList<ProductPriceHistory>> GetPriceHistoryAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductPriceHistory>> GetPriceHistoryByProductIdAsync(
        int productId,
        CancellationToken cancellationToken = default);

    Task<ProductPriceHistory?> GetPriceHistoryByIdAsync(
        int historyId,
        CancellationToken cancellationToken = default);
}
