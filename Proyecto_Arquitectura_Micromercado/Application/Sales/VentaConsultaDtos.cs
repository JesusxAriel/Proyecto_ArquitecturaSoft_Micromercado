namespace Proyecto_Arquitectura_Micromercado.Application.Sales;

public sealed record VentaResumenDto(
    int Id,
    string NroComprobante,
    DateTime FechaHora,
    string NombreCliente,
    string Estado,
    decimal Total);

public sealed record LoteConsumidoDto(
    string CodigoLote,
    int Cantidad);

public sealed record LineaDetalleVentaDto(
    int Cantidad,
    string Descripcion,
    decimal PrecioUnitario,
    decimal Subtotal,
    IReadOnlyList<LoteConsumidoDto> Lotes);

public sealed record VentaDetalleDto(
    int Id,
    string NroComprobante,
    DateTime FechaHora,
    string NitCiCliente,
    string RazonSocialCliente,
    string UsuarioCajero,
    string Estado,
    decimal Total,
    string? MotivoAnulacion,
    DateTime? FechaAnulacion,
    string? UsuarioAnulacion,
    IReadOnlyList<LineaDetalleVentaDto> Lineas);
