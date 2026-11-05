namespace PuntoMarket.Ventas.Application.Comprobantes;

public interface IComprobanteService
{
    Task<ComprobanteDto?> ObtenerAsync(
        int idVenta,
        CancellationToken cancellationToken = default);
}
