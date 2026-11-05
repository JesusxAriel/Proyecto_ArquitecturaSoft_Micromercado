namespace Proyecto_Arquitectura_Micromercado.Dtos.Reportes;

public sealed record FiltrosListadoVentasDto(
    DateOnly? Desde,
    DateOnly? Hasta,
    string? Estado,
    string? Cliente,
    int? Tope);

public sealed record FilaListadoVentasDto(
    int IdVenta,
    string Comprobante,
    DateTime Fecha,
    string NitCi,
    string RazonSocial,
    string Cajero,
    string Estado,
    decimal Monto);

public sealed record TotalesListadoVentasDto(
    int VentasCompletadas,
    int VentasAnuladas,
    decimal MontoTotalDeCompletadas,
    decimal PromedioPorVenta);

public sealed record RespuestaListadoVentasDto(
    IReadOnlyList<FilaListadoVentasDto> Filas,
    TotalesListadoVentasDto Totales,
    DateTime GeneradoEn,
    string GeneradoPor);

public sealed record ProblemaDeApiDto(string? Detail);
