using PuntoMarket.Ventas.Domain.Stock;

namespace PuntoMarket.Ventas.Application.Stock;

/// <summary>
/// Acceso a lotes participante en la misma unidad de trabajo que las ventas.
/// El adaptador debe proteger la lectura/descuento concurrente y rechazar restauraciones
/// que excedan la cantidad inicial mediante RestauracionExcedeCantidadInicialException.
/// </summary>
public interface ILoteRepository
{
    Task<IReadOnlyList<LoteDisponible>> ObtenerVigentesParaDescuentoAsync(int idProducto, CancellationToken cancellationToken = default);

    Task DescontarAsync(int idLote, int cantidad, CancellationToken cancellationToken = default);

    Task RestaurarAsync(int idLote, int cantidad, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LoteDisponible>> ObtenerVencidosConStockAsync(DateOnly hoy, CancellationToken cancellationToken = default);

    Task DesactivarAsync(int idLote, int? idUsuario, CancellationToken cancellationToken = default);
}
