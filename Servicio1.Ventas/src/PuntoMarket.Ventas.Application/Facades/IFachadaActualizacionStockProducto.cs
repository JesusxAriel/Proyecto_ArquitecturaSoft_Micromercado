using PuntoMarket.Ventas.Domain.Stock;

namespace PuntoMarket.Ventas.Application.Facades;

public interface IFachadaActualizacionStockProducto
{
    Task<IReadOnlyList<ConsumoDeLote>> DescontarAsync(
        int idProducto,
        int cantidad,
        CancellationToken cancellationToken = default);

    Task RestaurarAsync(
        IEnumerable<ConsumoDeLote> consumos,
        CancellationToken cancellationToken = default);

    Task<ResultadoBajaDeLotes> DarDeBajaLotesVencidosAsync(
        CancellationToken cancellationToken = default);

    Task<ResultadoBajaDeLotes> DarDeBajaLotesVencidosDelSistemaAsync(
        CancellationToken cancellationToken = default);
}
