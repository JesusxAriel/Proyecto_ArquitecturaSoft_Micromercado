namespace Proyecto_Arquitectura_Micromercado.Application.Comprobantes;

// Valores de VENTA.estado. Se comparan con la columna tal cual la guarda la base.
public static class EstadoDeVenta
{
    public const string Completada = "COMPLETADA";
    public const string Anulada = "ANULADA";
}

// Una fila de DETALLE_VENTA tal como se lee de la base. Como el stock se lleva por lote,
// una venta puede tener VARIAS filas del mismo producto (una por lote consumido).
public sealed record DetalleDeVenta(
    int IdProducto,
    string Descripcion,
    int Cantidad,
    decimal PrecioUnitario,
    decimal Subtotal);

// Lo que el puerto de lectura devuelve de una venta: cabecera y detalle sin agrupar.
public sealed record VentaParaComprobante(
    int IdVenta,
    string NroComprobante,
    DateTime FechaVenta,
    string NitCiCliente,
    string RazonSocialCliente,
    string Estado,
    decimal MontoTotal,
    IReadOnlyList<DetalleDeVenta> Detalle);

// Una linea del comprobante: todas las filas de un mismo producto sumadas en una sola.
public sealed record LineaComprobanteDto(
    int Cantidad,
    string Descripcion,
    decimal PrecioUnitario,
    decimal Importe);

// El comprobante listo para presentarse. Incluye lo que el Anexo 2 del entregable pide:
// datos generales, detalle, total, y la fecha y hora de generacion con el usuario que
// genero el reporte (que no es necesariamente quien registro la venta).
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
