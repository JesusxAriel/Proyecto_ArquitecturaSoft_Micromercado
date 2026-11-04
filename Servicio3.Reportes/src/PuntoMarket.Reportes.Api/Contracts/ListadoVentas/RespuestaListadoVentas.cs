using PuntoMarket.Reportes.Domain.ListadoVentas;

namespace PuntoMarket.Reportes.Api.Contracts.ListadoVentas;

public sealed record FilaDeVentaDto(
    int IdVenta,
    string Comprobante,
    DateTime Fecha,
    string NitCi,
    string RazonSocial,
    string Cajero,
    string Estado,
    decimal Monto);

public sealed record TotalesDeVentasDto(
    int VentasCompletadas,
    int VentasAnuladas,
    decimal MontoTotalDeCompletadas,
    decimal PromedioPorVenta);

public sealed record FiltrosDeVentasDto(DateOnly? Desde, DateOnly? Hasta, string? Estado, string? Cliente, int? Tope);

public sealed record RespuestaListadoVentas(
    IReadOnlyList<FilaDeVentaDto> Filas,
    TotalesDeVentasDto Totales,
    FiltrosDeVentasDto Filtros,
    DateTime GeneradoEn,
    string GeneradoPor)
{
    public static RespuestaListadoVentas Desde(ReporteListadoVentas reporte) =>
        new(
            reporte.Filas.Select(ConvertirFila).ToList(),
            ConvertirTotales(reporte.Totales),
            ConvertirFiltros(reporte.Filtros),
            reporte.GeneradoEn,
            reporte.GeneradoPor);

    private static FilaDeVentaDto ConvertirFila(FilaDeVenta fila) =>
        new(
            fila.IdVenta,
            fila.NroComprobante,
            fila.FechaVenta,
            fila.NitCi,
            fila.RazonSocial,
            fila.Cajero,
            Nombrar(fila.Estado),
            fila.MontoTotal);

    private static TotalesDeVentasDto ConvertirTotales(TotalesListadoVentas totales) =>
        new(
            totales.CantidadDeCompletadas,
            totales.CantidadDeAnuladas,
            totales.MontoTotalDeCompletadas,
            totales.PromedioPorVenta);

    private static FiltrosDeVentasDto ConvertirFiltros(FiltrosDeListadoVentas filtros) =>
        new(
            filtros.Rango.Desde,
            filtros.Rango.Hasta,
            filtros.Estado is { } estado ? Nombrar(estado) : null,
            filtros.Cliente,
            filtros.Tope);

    private static string Nombrar(EstadoDeVenta estado) => estado.ToString().ToUpperInvariant();
}
