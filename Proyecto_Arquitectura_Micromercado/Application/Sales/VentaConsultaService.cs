namespace Proyecto_Arquitectura_Micromercado.Application.Sales;

public sealed class VentaConsultaService(
    IVentaRepository ventaRepository,
    IVentaConsultaRepository consultaRepository) : IVentaConsultaService
{
    private const int LimiteHistorial = 50;

    public Task<decimal> ObtenerVentasDelDiaAsync(CancellationToken cancellationToken = default) =>
        ventaRepository.GetVentasDelDiaAsync(cancellationToken);

    public Task<IReadOnlyList<VentaResumenDto>> ListarAsync(
        bool incluirAnuladas,
        CancellationToken cancellationToken = default) =>
        consultaRepository.ListarAsync(incluirAnuladas, LimiteHistorial, cancellationToken);

    public async Task<VentaDetalleDto?> ObtenerDetalleAsync(
        int idVenta,
        CancellationToken cancellationToken = default)
    {
        if (idVenta <= 0)
        {
            return null;
        }

        var venta = await consultaRepository.ObtenerDetalleAsync(idVenta, cancellationToken);

        if (venta is null)
        {
            return null;
        }

        return new VentaDetalleDto(
            venta.Id,
            venta.NroComprobante,
            venta.FechaHora,
            venta.NitCiCliente,
            venta.RazonSocialCliente,
            venta.UsuarioCajero,
            venta.Estado,
            venta.Total,
            venta.EfectivoPagado,
            venta.Cambio,
            venta.MotivoAnulacion,
            venta.FechaAnulacion,
            venta.UsuarioAnulacion,
            AgruparPorProducto(venta.Filas));
    }

    private static IReadOnlyList<LineaDetalleVentaDto> AgruparPorProducto(
        IReadOnlyList<FilaDetalleVenta> filas) =>
        filas
            .GroupBy(fila => (fila.IdProducto, fila.PrecioUnitario))
            .Select(grupo => new LineaDetalleVentaDto(
                grupo.Sum(fila => fila.Cantidad),
                grupo.First().Descripcion,
                grupo.Key.PrecioUnitario,
                grupo.Sum(fila => fila.Subtotal)))
            .ToList();
}
