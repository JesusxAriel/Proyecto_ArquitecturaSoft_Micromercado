namespace Proyecto_Arquitectura_Micromercado.Application.Sales;

public sealed record NuevaVentaDto(
    int IdCliente,
    decimal EfectivoPagado,
    IReadOnlyList<LineaVentaDto> Lineas);

public sealed record LineaVentaDto(
    int IdProducto,
    int Cantidad);

public sealed record ResultadoVentaDto(
    int IdVenta,
    decimal Total);
