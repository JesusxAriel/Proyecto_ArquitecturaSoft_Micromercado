using PuntoMarket.Reportes.Domain.ValueObjects;

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

    public IReadOnlyList<PuntoDeTendencia> MontoPorDia() =>
        Completadas()
            .GroupBy(f => DateOnly.FromDateTime(f.FechaVenta))
            .OrderBy(g => g.Key)
            .Select(g => new PuntoDeTendencia(g.Key, g.Sum(f => f.MontoTotal)))
            .ToList();

    public IReadOnlyList<ParteDelTotal> ParticipacionPorCliente(int maximo) =>
        ParteDelTotal.TopConOtros(MontoPorCliente(), maximo);

    private IReadOnlyList<ParteDelTotal> MontoPorCliente() =>
        Completadas()
            .GroupBy(f => (f.NitCi, f.RazonSocial))
            .Select(g => new ParteDelTotal(g.Key.RazonSocial, g.Sum(f => f.MontoTotal)))
            .OrderByDescending(p => p.Monto)
            .ThenBy(p => p.Nombre)
            .ToList();

    private IEnumerable<FilaDeVenta> Completadas() =>
        Filas.Where(f => f.Estado == EstadoDeVenta.Completada);
}
