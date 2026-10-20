namespace Proyecto_Arquitectura_Micromercado.Application.Sales;

public interface IVentaConsultaService
{
    Task<decimal> ObtenerVentasDelDiaAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VentaResumenDto>> ListarAsync(
        bool incluirAnuladas,
        CancellationToken cancellationToken = default);

    Task<VentaDetalleDto?> ObtenerDetalleAsync(
        int idVenta,
        CancellationToken cancellationToken = default);

    Task InvalidarHistorialAsync(
    CancellationToken cancellationToken = default);
}
