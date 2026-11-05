namespace PuntoMarket.Reportes.Domain.ListadoVentas;

public sealed record TotalesListadoVentas(
    int CantidadDeCompletadas,
    int CantidadDeAnuladas,
    decimal MontoTotalDeCompletadas,
    decimal PromedioPorVenta,
    decimal VentaMasAlta = 0m)
{
    public static TotalesListadoVentas Calcular(IReadOnlyList<FilaDeVenta> filas)
    {
        var completadas = filas.Where(EsCompletada).ToList();
        var montoTotal = completadas.Sum(f => f.MontoTotal);
        return new(
            completadas.Count,
            filas.Count(EsAnulada),
            montoTotal,
            CalcularPromedio(montoTotal, completadas.Count),
            completadas.Count == 0 ? 0m : completadas.Max(f => f.MontoTotal));
    }

    private static bool EsCompletada(FilaDeVenta fila) => fila.Estado == EstadoDeVenta.Completada;

    private static bool EsAnulada(FilaDeVenta fila) => fila.Estado == EstadoDeVenta.Anulada;

    private static decimal CalcularPromedio(decimal montoTotal, int cantidad) =>
        cantidad == 0 ? 0m : decimal.Round(montoTotal / cantidad, 2, MidpointRounding.AwayFromZero);
}
