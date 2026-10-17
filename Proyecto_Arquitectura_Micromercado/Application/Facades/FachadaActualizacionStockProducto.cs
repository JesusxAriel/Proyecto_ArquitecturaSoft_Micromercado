using Proyecto_Arquitectura_Micromercado.Application.Common;
using Proyecto_Arquitectura_Micromercado.Application.Security;
using Proyecto_Arquitectura_Micromercado.Application.Stock;
using Proyecto_Arquitectura_Micromercado.Domain.Stock;

namespace Proyecto_Arquitectura_Micromercado.Application.Facades;

public sealed class FachadaActualizacionStockProducto(
    ILoteRepository loteRepository,
    TimeProvider reloj,
    IUnidadDeTrabajo unidad,
    IUsuarioActual usuarioActual) : IFachadaActualizacionStockProducto
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

    public Task<ResultadoBajaDeLotes> DarDeBajaLotesVencidosAsync(
        CancellationToken cancellationToken = default) =>
        DarDeBajaAsync(usuarioActual.Id, cancellationToken);

    public Task<ResultadoBajaDeLotes> DarDeBajaLotesVencidosDelSistemaAsync(
        CancellationToken cancellationToken = default) =>
        DarDeBajaAsync(null, cancellationToken);

    private async Task<ResultadoBajaDeLotes> DarDeBajaAsync(
        int? idUsuario,
        CancellationToken cancellationToken)
    {
        await unidad.IniciarAsync(cancellationToken);
        try
        {
            var hoy = DateOnly.FromDateTime(reloj.GetLocalNow().DateTime);
            var vencidos = await loteRepository.ObtenerVencidosConStockAsync(hoy, cancellationToken);

            foreach (var lote in vencidos)
            {
                await loteRepository.DesactivarAsync(lote.IdLote, idUsuario, cancellationToken);
            }

            await unidad.ConfirmarAsync(cancellationToken);
            return new ResultadoBajaDeLotes(vencidos.Count, vencidos.Sum(l => l.CantidadDisponible));
        }
        catch
        {
            await unidad.RevertirAsync(CancellationToken.None);
            throw;
        }
    }
}
