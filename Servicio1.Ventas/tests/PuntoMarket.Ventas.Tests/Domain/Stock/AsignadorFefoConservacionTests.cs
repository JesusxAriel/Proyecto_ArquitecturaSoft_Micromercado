using PuntoMarket.Ventas.Domain.Stock;

namespace PuntoMarket.Ventas.Tests.Domain.Stock;

public class AsignadorFefoConservacionTests
{
    private static readonly DateOnly Hoy = new(2026, 6, 15);

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(7)]
    [InlineData(12)]
    public void La_asignacion_conserva_cantidades_por_lote_y_no_muta_la_entrada(int cantidad)
    {
        LoteDisponible[] lotes =
        [
            new(9, 5, null),
            new(7, 4, Hoy.AddDays(2)),
            new(3, 3, Hoy),
            new(1, 99, Hoy.AddDays(-1)),
            new(2, 0, Hoy)
        ];
        var originales = lotes.ToArray();

        var consumos = AsignadorFefo.Asignar(lotes, cantidad, Hoy);

        Assert.Equal(cantidad, consumos.Sum(c => c.Cantidad));
        Assert.Equal(consumos.Count, consumos.Select(c => c.IdLote).Distinct().Count());
        Assert.All(consumos, consumo =>
        {
            var lote = Assert.Single(lotes, l => l.IdLote == consumo.IdLote);
            Assert.InRange(consumo.Cantidad, 1, lote.CantidadDisponible);
            var restante = lote.CantidadDisponible - consumo.Cantidad;
            Assert.Equal(lote.CantidadDisponible, restante + consumo.Cantidad);
            Assert.True(lote.FechaVencimiento is null || lote.FechaVencimiento >= Hoy);
        });
        Assert.Equal(originales, lotes);
    }

    [Fact]
    public void La_insuficiencia_no_modifica_las_cantidades_originales()
    {
        LoteDisponible[] lotes = [new(1, 2, Hoy), new(2, 3, null)];
        var originales = lotes.ToArray();

        Assert.Throws<StockInsuficienteException>(() => AsignadorFefo.Asignar(lotes, 6, Hoy));

        Assert.Equal(originales, lotes);
    }

    [Fact]
    public void Una_entrada_nula_se_rechaza()
    {
        Assert.Throws<ArgumentNullException>(() => AsignadorFefo.Asignar(null!, 1, Hoy));
    }
}
