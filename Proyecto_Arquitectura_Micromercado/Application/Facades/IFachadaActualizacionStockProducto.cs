using Proyecto_Arquitectura_Micromercado.Domain.Stock;

namespace Proyecto_Arquitectura_Micromercado.Application.Facades;

public interface IFachadaActualizacionStockProducto
{
    Task<IReadOnlyList<ConsumoDeLote>> DescontarAsync(
        int idProducto,
        int cantidad,
        CancellationToken cancellationToken = default);

    Task RestaurarAsync(
        IEnumerable<ConsumoDeLote> consumos,
        CancellationToken cancellationToken = default);
}
