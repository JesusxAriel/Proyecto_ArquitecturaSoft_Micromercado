using Proyecto_Arquitectura_Micromercado.Application.Comprobantes;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.EnMemoria;

// Adaptador en memoria del puerto IComprobanteRepository, para probar el comprobante sin
// base de datos. Devuelve las ventas que se le cargan, tal cual, con el detalle sin
// agrupar, igual que el adaptador MySQL.
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
