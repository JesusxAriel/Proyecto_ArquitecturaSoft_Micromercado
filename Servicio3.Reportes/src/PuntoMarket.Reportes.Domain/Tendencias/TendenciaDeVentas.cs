using PuntoMarket.Reportes.Domain.ValueObjects;

namespace PuntoMarket.Reportes.Domain.Tendencias;

public sealed record TendenciaDeVentas(AgrupacionDeTendencia Agrupacion, IReadOnlyList<PuntoDeTendencia> Puntos)
{
    public const int DiasMaximosParaAgrupacionDiaria = 45;
    private const int DiasPorSemana = 7;

    public bool PuedeGraficarse => Puntos.Count >= 2;

    public static TendenciaDeVentas Vacia() => new(AgrupacionDeTendencia.Diaria, []);

    public static TendenciaDeVentas Desde(IReadOnlyList<PuntoDeTendencia> montosPorDia)
    {
        if (montosPorDia.Count == 0)
            return Vacia();

        var ordenados = montosPorDia.OrderBy(p => p.Fecha).ToList();
        var dias = ordenados[^1].Fecha.DayNumber - ordenados[0].Fecha.DayNumber + 1;

        return dias > DiasMaximosParaAgrupacionDiaria
            ? new TendenciaDeVentas(AgrupacionDeTendencia.Semanal, AgruparPorSemana(ordenados))
            : new TendenciaDeVentas(AgrupacionDeTendencia.Diaria, CompletarDias(ordenados));
    }

    private static IReadOnlyList<PuntoDeTendencia> CompletarDias(IReadOnlyList<PuntoDeTendencia> ordenados)
    {
        var porFecha = ordenados.GroupBy(p => p.Fecha).ToDictionary(g => g.Key, g => g.Sum(p => p.Monto));
        var primero = ordenados[0].Fecha;
        var ultimo = ordenados[^1].Fecha;

        return Enumerable
            .Range(0, ultimo.DayNumber - primero.DayNumber + 1)
            .Select(desplazamiento => primero.AddDays(desplazamiento))
            .Select(fecha => new PuntoDeTendencia(fecha, porFecha.GetValueOrDefault(fecha)))
            .ToList();
    }

    private static IReadOnlyList<PuntoDeTendencia> AgruparPorSemana(IReadOnlyList<PuntoDeTendencia> ordenados)
    {
        var porSemana = ordenados
            .GroupBy(p => InicioDeSemana(p.Fecha))
            .ToDictionary(g => g.Key, g => g.Sum(p => p.Monto));
        var primera = InicioDeSemana(ordenados[0].Fecha);
        var ultima = InicioDeSemana(ordenados[^1].Fecha);
        var semanas = (ultima.DayNumber - primera.DayNumber) / DiasPorSemana + 1;

        return Enumerable
            .Range(0, semanas)
            .Select(indice => primera.AddDays(indice * DiasPorSemana))
            .Select(inicio => new PuntoDeTendencia(inicio, porSemana.GetValueOrDefault(inicio)))
            .ToList();
    }

    private static DateOnly InicioDeSemana(DateOnly fecha)
    {
        var desplazamiento = ((int)fecha.DayOfWeek + 6) % DiasPorSemana;
        return fecha.AddDays(-desplazamiento);
    }
}
