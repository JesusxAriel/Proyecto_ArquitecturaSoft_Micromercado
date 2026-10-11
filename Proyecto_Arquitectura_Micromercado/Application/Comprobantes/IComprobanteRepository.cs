namespace Proyecto_Arquitectura_Micromercado.Application.Comprobantes;

public interface IComprobanteRepository
{
    Task<VentaParaComprobante?> ObtenerPorIdVentaAsync(
        int idVenta,
        CancellationToken cancellationToken = default);
}
