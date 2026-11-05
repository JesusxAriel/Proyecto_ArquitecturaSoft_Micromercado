using PuntoMarket.Reportes.Domain.Tendencias;

namespace PuntoMarket.Reportes.Domain.VentasPorCategoria;

public sealed record ReporteVentasPorCategoria(
    IReadOnlyList<FilaDeCategoria> Filas,
    TotalesPorCategoria Totales,
    FiltrosDeVentasPorCategoria Filtros,
    DateTime GeneradoEn,
    string GeneradoPor,
    TendenciaDeVentas? Tendencia = null,
    IReadOnlyList<FilaDeProducto>? ProductosMasVendidos = null)
{
    public static ReporteVentasPorCategoria Crear(
        IReadOnlyList<FilaDeCategoria> filas,
        FiltrosDeVentasPorCategoria filtros,
        DateTime generadoEn,
        string generadoPor,
        TendenciaDeVentas? tendencia = null,
        IReadOnlyList<FilaDeProducto>? productosMasVendidos = null) =>
        new(filas, TotalesPorCategoria.Calcular(filas), filtros, generadoEn, generadoPor, tendencia, productosMasVendidos);

    public ReporteVentasPorCategoria ConDetalle(
        TendenciaDeVentas tendencia,
        IReadOnlyList<FilaDeProducto> productosMasVendidos) =>
        this with { Tendencia = tendencia, ProductosMasVendidos = productosMasVendidos };
}
