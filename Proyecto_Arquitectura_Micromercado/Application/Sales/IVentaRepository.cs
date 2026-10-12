using Proyecto_Arquitectura_Micromercado.Domain.Sales;

namespace Proyecto_Arquitectura_Micromercado.Application.Sales;

public interface IVentaRepository
{
    Task<Venta?> GetByIdAsync(int idVenta, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Venta>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<(int IdVenta, string NroComprobante, decimal Total)> RegistrarVentaConSpAsync(NuevaVentaDto dto, int idUsuario, CancellationToken cancellationToken = default);
    Task<bool> AnularVentaConSpAsync(int idVenta, string motivo, int idUsuarioAnulacion, CancellationToken cancellationToken = default);
    Task<decimal> GetVentasDelDiaAsync(CancellationToken cancellationToken = default);
}
