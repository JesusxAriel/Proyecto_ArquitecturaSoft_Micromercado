namespace Proyecto_Arquitectura_Micromercado.Application.Comprobantes;

public sealed class ComprobanteService(IComprobanteRepository repositorio) : IComprobanteService
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
            MontoEnLetras.Convertir(venta.MontoTotal),
            venta.FechaVenta,
            venta.UsuarioCajero);
    }

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
