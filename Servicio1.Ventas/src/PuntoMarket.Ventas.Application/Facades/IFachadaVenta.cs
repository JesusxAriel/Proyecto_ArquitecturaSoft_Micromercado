using PuntoMarket.Ventas.Application.Sales;

namespace PuntoMarket.Ventas.Application.Facades;

public interface IFachadaVenta
{
    Task<ResultadoVentaDto> RegistrarVentaAsync(
        NuevaVentaDto venta,
        CancellationToken cancellationToken = default);
}
