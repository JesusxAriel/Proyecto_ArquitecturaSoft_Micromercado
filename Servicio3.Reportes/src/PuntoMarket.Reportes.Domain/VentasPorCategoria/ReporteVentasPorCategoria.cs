namespace PuntoMarket.Reportes.Domain.VentasPorCategoria;

public sealed record ReporteVentasPorCategoria(
    IReadOnlyList<FilaDeCategoria> Filas,
    TotalesPorCategoria Totales,
    FiltrosDeVentasPorCategoria Filtros,
    DateTime GeneradoEn,
    string GeneradoPor)
{
    public static ReporteVentasPorCategoria Crear(
        IReadOnlyList<FilaDeCategoria> filas,
        FiltrosDeVentasPorCategoria filtros,
        DateTime generadoEn,
        string generadoPor) =>
        new(filas, TotalesPorCategoria.Calcular(filas), filtros, generadoEn, generadoPor);
}
