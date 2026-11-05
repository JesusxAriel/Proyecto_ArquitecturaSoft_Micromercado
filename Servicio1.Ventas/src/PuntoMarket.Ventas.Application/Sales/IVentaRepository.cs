using PuntoMarket.Ventas.Domain.Sales;

namespace PuntoMarket.Ventas.Application.Sales;

/// <summary>Persistencia de ventas participante en la unidad de trabajo del caso de uso.</summary>
public interface IVentaRepository
{
    Task<Venta?> GetByIdAsync(int idVenta, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Venta>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<decimal> GetVentasDelDiaAsync(CancellationToken cancellationToken = default);

    Task<int> InsertarCabeceraAsync(int idCliente, int idUsuario, CancellationToken cancellationToken = default);
    Task InsertarDetalleAsync(int idVenta, int idProducto, int idLote, int cantidad, decimal precioUnitario, CancellationToken cancellationToken = default);
    Task CerrarVentaAsync(int idVenta, string nroComprobante, decimal montoTotal, CancellationToken cancellationToken = default);
    /// <summary>Lee la venta y sus lotes con protección frente a anulaciones concurrentes dentro de la transacción.</summary>
    Task<Venta?> ObtenerParaAnularAsync(int idVenta, CancellationToken cancellationToken = default);
    Task MarcarAnuladaAsync(int idVenta, string motivo, int idUsuarioAnulacion, CancellationToken cancellationToken = default);
}
