using Proyecto_Arquitectura_Micromercado.Domain.Sales;

namespace Proyecto_Arquitectura_Micromercado.Application.Sales;

public interface IVentaService
{
    Task<int> RegistrarVentaAsync(Venta venta, CancellationToken cancellationToken = default);
    Task<bool> AnularVentaAsync(int idVenta, string motivo, CancellationToken cancellationToken = default);
    Task<Venta?> ObtenerPorIdAsync(int idVenta, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Venta>> ObtenerTodasAsync(CancellationToken cancellationToken = default);
}