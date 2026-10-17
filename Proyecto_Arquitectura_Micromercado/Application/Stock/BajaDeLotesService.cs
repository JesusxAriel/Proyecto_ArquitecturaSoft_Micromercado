using Proyecto_Arquitectura_Micromercado.Application.Facades;
using Proyecto_Arquitectura_Micromercado.Domain.Stock;

namespace Proyecto_Arquitectura_Micromercado.Application.Stock;

public sealed class BajaDeLotesService(IFachadaActualizacionStockProducto fachada) : IBajaDeLotesService
{
    public Task<ResultadoBajaDeLotes> DarDeBajaVencidosAsync(CancellationToken cancellationToken = default) =>
        fachada.DarDeBajaLotesVencidosAsync(cancellationToken);

    public Task<ResultadoBajaDeLotes> DarDeBajaVencidosDelSistemaAsync(CancellationToken cancellationToken = default) =>
        fachada.DarDeBajaLotesVencidosDelSistemaAsync(cancellationToken);
}
