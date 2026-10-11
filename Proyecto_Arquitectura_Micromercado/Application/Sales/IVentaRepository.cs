using Proyecto_Arquitectura_Micromercado.Domain.Sales;

namespace Proyecto_Arquitectura_Micromercado.Application.Sales;

public interface IVentaRepository
{
    Task<int> CrearVentaAsync(Venta venta, CancellationToken cancellationToken = default);
    Task CrearDetalleVentaAsync(DetalleVenta detalle, CancellationToken cancellationToken = default);
    Task<Venta?> GetByIdAsync(int idVenta, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Venta>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> CambiarEstadoAnuladoAsync(int idVenta, string motivo, CancellationToken cancellationToken = default);
}