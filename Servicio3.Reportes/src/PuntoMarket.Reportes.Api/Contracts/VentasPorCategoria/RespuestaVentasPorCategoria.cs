using PuntoMarket.Reportes.Domain.VentasPorCategoria;

namespace PuntoMarket.Reportes.Api.Contracts.VentasPorCategoria;

public sealed record FilaDeCategoriaDto(int IdCategoria, string Categoria, long UnidadesVendidas, decimal MontoVendido);

public sealed record TotalesDto(long TotalUnidades, decimal TotalMonto, string? CategoriaLider);

public sealed record FiltrosDto(DateOnly? Desde, DateOnly? Hasta, int? IdCategoria, int? Top);

public sealed record RespuestaVentasPorCategoria(
    IReadOnlyList<FilaDeCategoriaDto> Filas,
    TotalesDto Totales,
    FiltrosDto Filtros,
    DateTime GeneradoEn,
    string GeneradoPor)
{
    public static RespuestaVentasPorCategoria Desde(ReporteVentasPorCategoria reporte) =>
        new(
            reporte.Filas.Select(f => new FilaDeCategoriaDto(f.IdCategoria, f.NombreCategoria, f.UnidadesVendidas, f.MontoVendido)).ToList(),
            new TotalesDto(reporte.Totales.TotalUnidades, reporte.Totales.TotalMonto, reporte.Totales.CategoriaLider?.NombreCategoria),
            new FiltrosDto(reporte.Filtros.Rango.Desde, reporte.Filtros.Rango.Hasta, reporte.Filtros.IdCategoria, reporte.Filtros.Top),
            reporte.GeneradoEn,
            reporte.GeneradoPor);
}
