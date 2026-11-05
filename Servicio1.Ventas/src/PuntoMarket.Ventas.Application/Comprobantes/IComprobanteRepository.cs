namespace PuntoMarket.Ventas.Application.Comprobantes;

public interface IComprobanteRepository
{
    Task<VentaParaComprobante?> ObtenerPorIdVentaAsync(
        int idVenta,
        CancellationToken cancellationToken = default);
}
