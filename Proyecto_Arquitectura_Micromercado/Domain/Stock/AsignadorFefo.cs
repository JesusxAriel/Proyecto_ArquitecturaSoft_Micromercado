namespace Proyecto_Arquitectura_Micromercado.Domain.Stock;

public static class AsignadorFefo
{
    public static IReadOnlyList<ConsumoDeLote> Asignar(IEnumerable<LoteDisponible> lotes, int cantidad, DateOnly hoy)
    {
        ArgumentNullException.ThrowIfNull(lotes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cantidad);

        var ordenados = lotes
            .Where(l => l.CantidadDisponible > 0)
            .Where(l => l.FechaVencimiento is null || l.FechaVencimiento.Value >= hoy)
            .OrderBy(l => l.FechaVencimiento is null)
            .ThenBy(l => l.FechaVencimiento)
            .ThenBy(l => l.IdLote);

        var consumos = new List<ConsumoDeLote>();
        var restante = cantidad;

        foreach (var lote in ordenados)
        {
            var tomado = Math.Min(restante, lote.CantidadDisponible);
            consumos.Add(new ConsumoDeLote(lote.IdLote, tomado));
            restante -= tomado;

            if (restante == 0) return consumos;
        }

        throw new StockInsuficienteException();
    }
}
