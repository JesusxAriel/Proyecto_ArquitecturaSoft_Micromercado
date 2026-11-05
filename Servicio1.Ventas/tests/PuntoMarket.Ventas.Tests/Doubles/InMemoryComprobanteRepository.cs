using PuntoMarket.Ventas.Application.Comprobantes;

namespace PuntoMarket.Ventas.Tests;

public sealed class InMemoryComprobanteRepository : IComprobanteRepository
{
    private readonly List<VentaParaComprobante> ventas = [];

    public InMemoryComprobanteRepository Agregar(VentaParaComprobante venta)
    {
        ventas.Add(venta);
        return this;
    }

    public Task<VentaParaComprobante?> ObtenerPorIdVentaAsync(
        int idVenta,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ventas.FirstOrDefault(venta => venta.IdVenta == idVenta));
}
