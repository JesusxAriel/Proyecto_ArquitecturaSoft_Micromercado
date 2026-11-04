namespace PuntoMarket.Reportes.Domain.ListadoVentas;

public sealed record ReporteListadoVentas(
    IReadOnlyList<FilaDeVenta> Filas,
    TotalesListadoVentas Totales,
    FiltrosDeListadoVentas Filtros,
    DateTime GeneradoEn,
    string GeneradoPor)
{
    public static ReporteListadoVentas Crear(
        IReadOnlyList<FilaDeVenta> filas,
        FiltrosDeListadoVentas filtros,
        DateTime generadoEn,
        string generadoPor) =>
        new(filas, TotalesListadoVentas.Calcular(filas), filtros, generadoEn, generadoPor);
}
