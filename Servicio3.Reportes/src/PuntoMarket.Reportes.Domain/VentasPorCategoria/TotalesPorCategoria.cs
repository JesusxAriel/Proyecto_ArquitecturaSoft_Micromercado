namespace PuntoMarket.Reportes.Domain.VentasPorCategoria;

public sealed record TotalesPorCategoria(long TotalUnidades, decimal TotalMonto, FilaDeCategoria? CategoriaLider)
{
    public static TotalesPorCategoria Calcular(IReadOnlyList<FilaDeCategoria> filas) =>
        new(
            filas.Sum(f => f.UnidadesVendidas),
            filas.Sum(f => f.MontoVendido),
            filas.MaxBy(f => f.MontoVendido));
}
