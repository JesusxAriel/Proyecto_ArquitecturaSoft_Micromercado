using Proyecto_Arquitectura_Micromercado.Domain.Products;

namespace Proyecto_Arquitectura_Micromercado.Application.Products;

public interface IConRegistroHistorial
{
    Task AddPriceHistoryAsync(
        ProductPriceHistory history,
        CancellationToken cancellationToken = default);
}
