using Proyecto_Arquitectura_Micromercado.Domain.Stock;

namespace Proyecto_Arquitectura_Micromercado.Application.Stock;

public interface IBajaDeLotesService
{
    Task<ResultadoBajaDeLotes> DarDeBajaVencidosAsync(CancellationToken cancellationToken = default);
}
