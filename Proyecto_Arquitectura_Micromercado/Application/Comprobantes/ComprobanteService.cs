using Proyecto_Arquitectura_Micromercado.Application.Security;

namespace Proyecto_Arquitectura_Micromercado.Application.Comprobantes;

// El reloj llega por TimeProvider para que las pruebas puedan fijar la fecha y hora de
// generacion sin depender del momento en que corren.
public sealed class ComprobanteService(
    IComprobanteRepository repositorio,
    IUsuarioActual usuarioActual,
    TimeProvider reloj) : IComprobanteService
{
    public async Task<ComprobanteDto?> ObtenerAsync(
        int idVenta,
        CancellationToken cancellationToken = default)
    {
        if (idVenta <= 0)
        {
            return null;
        }

        var venta = await repositorio.ObtenerPorIdVentaAsync(idVenta, cancellationToken);

        if (venta is null)
        {
            return null;
        }

        return new ComprobanteDto(
            venta.NroComprobante,
            venta.FechaVenta,
            venta.NitCiCliente,
            venta.RazonSocialCliente,
            venta.Estado,
            AgruparPorProducto(venta.Detalle),
            venta.MontoTotal,
            reloj.GetLocalNow().DateTime,
            usuarioActual.NombreUsuario);
    }

    // Una venta puede traer varias filas del mismo producto, una por cada lote del que
    // salio la mercaderia. El comprobante las muestra como una sola linea con la cantidad
    // y el importe sumados. Se agrupa por producto y precio, y las lineas conservan el
    // orden en que aparece cada producto por primera vez.
    private static IReadOnlyList<LineaComprobanteDto> AgruparPorProducto(
        IReadOnlyList<DetalleDeVenta> detalle) =>
        detalle
            .GroupBy(fila => (fila.IdProducto, fila.PrecioUnitario))
            .Select(grupo => new LineaComprobanteDto(
                grupo.Sum(fila => fila.Cantidad),
                grupo.First().Descripcion,
                grupo.Key.PrecioUnitario,
                grupo.Sum(fila => fila.Subtotal)))
            .ToList();
}
