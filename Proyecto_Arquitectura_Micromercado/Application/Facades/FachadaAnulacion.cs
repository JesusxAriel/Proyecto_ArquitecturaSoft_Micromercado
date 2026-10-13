using Proyecto_Arquitectura_Micromercado.Application.Common;
using Proyecto_Arquitectura_Micromercado.Application.Sales;
using Proyecto_Arquitectura_Micromercado.Application.Security;
using Proyecto_Arquitectura_Micromercado.Domain.Sales;
using Proyecto_Arquitectura_Micromercado.Domain.Stock;

namespace Proyecto_Arquitectura_Micromercado.Application.Facades;

public sealed class FachadaAnulacion(
    IUnidadDeTrabajo unidad,
    IVentaRepository ventaRepository,
    IFachadaActualizacionStockProducto stock,
    IUsuarioActual usuarioActual) : IFachadaAnulacion
{
    public async Task AnularVentaAsync(
        int idVenta,
        string motivo,
        CancellationToken cancellationToken = default)
    {
        if (idVenta <= 0)
        {
            throw new InvalidOperationException("Debe indicar la venta que se desea anular.");
        }

        if (string.IsNullOrWhiteSpace(motivo))
        {
            throw new InvalidOperationException("Debe indicar el motivo de la anulación.");
        }

        await unidad.IniciarAsync(cancellationToken);
        try
        {
            var venta = await ventaRepository.ObtenerParaAnularAsync(idVenta, cancellationToken);
            if (venta is null || venta.Estado != EstadoVenta.Completada)
            {
                throw new InvalidOperationException("La venta no existe o ya fue anulada.");
            }

            await stock.RestaurarAsync(
                venta.Detalles.Select(d => new ConsumoDeLote(d.IdLote, d.Cantidad)),
                cancellationToken);
            await ventaRepository.MarcarAnuladaAsync(idVenta, motivo.Trim(), usuarioActual.Id, cancellationToken);

            await unidad.ConfirmarAsync(cancellationToken);
        }
        catch
        {
            await unidad.RevertirAsync(CancellationToken.None);
            throw;
        }
    }
}
