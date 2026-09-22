using Proyecto_Arquitectura_Micromercado.Domain.Products;
using System.Data;

namespace Proyecto_Arquitectura_Micromercado.Application.Products;

public interface IPriceHistoryRepository : IConHistorialPrecios, IConRegistroHistorial
{
    // Permite al repositorio de producto delegar inserciones dentro de una transacción
    Task AddPriceHistoryAsync(IDbConnection connection, IDbTransaction? transaction, ProductPriceHistory history, CancellationToken cancellationToken = default);
}
