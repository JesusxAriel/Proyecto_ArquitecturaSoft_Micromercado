namespace Proyecto_Arquitectura_Micromercado.Application.Comprobantes;

public static class EstadoDeVenta
{
    public const string Completada = "COMPLETADA";
    public const string Anulada = "ANULADA";
}

public sealed record DetalleDeVenta(
    int IdProducto,
    string Descripcion,
    int Cantidad,
    decimal PrecioUnitario,
    decimal Subtotal);

public sealed record VentaParaComprobante(
    int IdVenta,
    string NroComprobante,
    DateTime FechaVenta,
    string NitCiCliente,
    string RazonSocialCliente,
    string Estado,
    decimal MontoTotal,
    string UsuarioCajero,
    IReadOnlyList<DetalleDeVenta> Detalle);

public sealed record LineaComprobanteDto(
    int Cantidad,
    string Descripcion,
    decimal PrecioUnitario,
    decimal Importe);

public sealed record ComprobanteDto(
    string NroComprobante,
    DateTime FechaVenta,
    string NitCiCliente,
    string RazonSocialCliente,
    string Estado,
    IReadOnlyList<LineaComprobanteDto> Lineas,
    decimal Total,
    string TotalEnLetras,
    DateTime FechaGeneracion,
    string UsuarioGenerador)
{
    public bool EstaAnulada => Estado == EstadoDeVenta.Anulada;
}
