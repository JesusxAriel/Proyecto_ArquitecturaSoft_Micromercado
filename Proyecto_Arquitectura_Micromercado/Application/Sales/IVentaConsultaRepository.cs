namespace Proyecto_Arquitectura_Micromercado.Application.Sales;

public interface IVentaConsultaRepository
{
    Task<IReadOnlyList<VentaResumenDto>> ListarAsync(
        bool incluirAnuladas,
        int limite,
        CancellationToken cancellationToken = default);

    Task<VentaDetalleConFilasDto?> ObtenerDetalleAsync(
        int idVenta,
        CancellationToken cancellationToken = default);
}

public sealed record FilaDetalleVenta(
    int IdProducto,
    string Descripcion,
    int Cantidad,
    decimal PrecioUnitario,
    decimal Subtotal);

public sealed record VentaDetalleConFilasDto(
    int Id,
    string NroComprobante,
    DateTime FechaHora,
    string NitCiCliente,
    string RazonSocialCliente,
    string UsuarioCajero,
    string Estado,
    decimal Total,
    decimal EfectivoPagado,
    decimal Cambio,
    string? MotivoAnulacion,
    DateTime? FechaAnulacion,
    string? UsuarioAnulacion,
    IReadOnlyList<FilaDetalleVenta> Filas);
