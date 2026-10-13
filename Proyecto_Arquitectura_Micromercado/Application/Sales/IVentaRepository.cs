using Proyecto_Arquitectura_Micromercado.Domain.Sales;

namespace Proyecto_Arquitectura_Micromercado.Application.Sales;

public interface IVentaRepository
{
    Task<Venta?> GetByIdAsync(int idVenta, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Venta>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<(int IdVenta, string NroComprobante, decimal Total)> RegistrarVentaConSpAsync(NuevaVentaDto dto, int idUsuario, CancellationToken cancellationToken = default);
    Task<bool> AnularVentaConSpAsync(int idVenta, string motivo, int idUsuarioAnulacion, CancellationToken cancellationToken = default);
    Task<decimal> GetVentasDelDiaAsync(CancellationToken cancellationToken = default);

    Task<int> InsertarCabeceraAsync(int idCliente, int idUsuario, CancellationToken cancellationToken = default);
    Task InsertarDetalleAsync(int idVenta, int idProducto, int idLote, int cantidad, decimal precioUnitario, CancellationToken cancellationToken = default);
    Task CerrarVentaAsync(int idVenta, string nroComprobante, decimal montoTotal, decimal efectivoPagado, CancellationToken cancellationToken = default);
    Task<Venta?> ObtenerParaAnularAsync(int idVenta, CancellationToken cancellationToken = default);
    Task MarcarAnuladaAsync(int idVenta, string motivo, int idUsuarioAnulacion, CancellationToken cancellationToken = default);
}
