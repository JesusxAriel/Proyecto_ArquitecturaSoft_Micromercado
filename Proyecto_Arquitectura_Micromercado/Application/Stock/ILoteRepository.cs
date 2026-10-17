using Proyecto_Arquitectura_Micromercado.Domain.Stock;

namespace Proyecto_Arquitectura_Micromercado.Application.Stock;

public interface ILoteRepository
{
    Task<IReadOnlyList<LoteDisponible>> ObtenerVigentesParaDescuentoAsync(int idProducto, CancellationToken cancellationToken = default);

    Task DescontarAsync(int idLote, int cantidad, CancellationToken cancellationToken = default);

    Task RestaurarAsync(int idLote, int cantidad, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LoteDisponible>> ObtenerVencidosConStockAsync(DateOnly hoy, CancellationToken cancellationToken = default);

    Task DesactivarAsync(int idLote, int? idUsuario, CancellationToken cancellationToken = default);
}
