using Proyecto_Arquitectura_Micromercado.Application.Stock;
using Proyecto_Arquitectura_Micromercado.Domain.Stock;

namespace Proyecto_Arquitectura_Micromercado.Application.Facades;

public sealed class FachadaActualizacionStockProducto(
    ILoteRepository loteRepository,
    TimeProvider reloj) : IFachadaActualizacionStockProducto
{
    public async Task<IReadOnlyList<ConsumoDeLote>> DescontarAsync(
        int idProducto,
        int cantidad,
        CancellationToken cancellationToken = default)
    {
        var lotes = await loteRepository.ObtenerVigentesParaDescuentoAsync(idProducto, cancellationToken);
        var hoy = DateOnly.FromDateTime(reloj.GetLocalNow().DateTime);
        var consumos = AsignadorFefo.Asignar(lotes, cantidad, hoy);

        foreach (var consumo in consumos)
        {
            await loteRepository.DescontarAsync(consumo.IdLote, consumo.Cantidad, cancellationToken);
        }

        return consumos;
    }

    public async Task RestaurarAsync(
        IEnumerable<ConsumoDeLote> consumos,
        CancellationToken cancellationToken = default)
    {
        foreach (var consumo in consumos)
        {
            await loteRepository.RestaurarAsync(consumo.IdLote, consumo.Cantidad, cancellationToken);
        }
    }
}
