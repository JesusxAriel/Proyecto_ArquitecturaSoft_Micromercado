using Proyecto_Arquitectura_Micromercado.Domain.Stock;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class AsignadorFefoTests
{
    private static readonly DateOnly Hoy = new(2026, 6, 15);

    [Fact]
    public void Un_lote_que_alcanza_cubre_toda_la_cantidad()
    {
        var lotes = new[] { new LoteDisponible(1, 10, Hoy.AddDays(30)) };

        var consumos = AsignadorFefo.Asignar(lotes, 4, Hoy);

        Assert.Equal([new ConsumoDeLote(1, 4)], consumos);
    }

    [Fact]
    public void Reparte_entre_dos_lotes_en_orden_de_vencimiento()
    {
        var lotes = new[]
        {
            new LoteDisponible(1, 5, Hoy.AddDays(60)),
            new LoteDisponible(2, 3, Hoy.AddDays(10))
        };

        var consumos = AsignadorFefo.Asignar(lotes, 6, Hoy);

        Assert.Equal([new ConsumoDeLote(2, 3), new ConsumoDeLote(1, 3)], consumos);
    }

    [Fact]
    public void Los_lotes_sin_vencimiento_van_al_final()
    {
        var lotes = new[]
        {
            new LoteDisponible(1, 5, null),
            new LoteDisponible(2, 5, Hoy.AddDays(90))
        };

        var consumos = AsignadorFefo.Asignar(lotes, 8, Hoy);

        Assert.Equal([new ConsumoDeLote(2, 5), new ConsumoDeLote(1, 3)], consumos);
    }

    [Fact]
    public void Ignora_los_lotes_vencidos()
    {
        var lotes = new[]
        {
            new LoteDisponible(1, 10, Hoy.AddDays(-1)),
            new LoteDisponible(2, 10, Hoy.AddDays(5))
        };

        var consumos = AsignadorFefo.Asignar(lotes, 4, Hoy);

        Assert.Equal([new ConsumoDeLote(2, 4)], consumos);
    }

    [Fact]
    public void Acepta_el_lote_que_vence_hoy()
    {
        var lotes = new[]
        {
            new LoteDisponible(1, 10, Hoy),
            new LoteDisponible(2, 10, Hoy.AddDays(5))
        };

        var consumos = AsignadorFefo.Asignar(lotes, 4, Hoy);

        Assert.Equal([new ConsumoDeLote(1, 4)], consumos);
    }

    [Fact]
    public void Ignora_los_lotes_con_cantidad_cero()
    {
        var lotes = new[]
        {
            new LoteDisponible(1, 0, Hoy.AddDays(1)),
            new LoteDisponible(2, 10, Hoy.AddDays(5))
        };

        var consumos = AsignadorFefo.Asignar(lotes, 3, Hoy);

        Assert.Equal([new ConsumoDeLote(2, 3)], consumos);
    }

    [Fact]
    public void Desempata_por_id_de_lote_con_el_mismo_vencimiento()
    {
        var lotes = new[]
        {
            new LoteDisponible(7, 5, Hoy.AddDays(10)),
            new LoteDisponible(3, 5, Hoy.AddDays(10))
        };

        var consumos = AsignadorFefo.Asignar(lotes, 7, Hoy);

        Assert.Equal([new ConsumoDeLote(3, 5), new ConsumoDeLote(7, 2)], consumos);
    }

    [Fact]
    public void Desempata_por_id_de_lote_entre_lotes_sin_vencimiento()
    {
        var lotes = new[]
        {
            new LoteDisponible(9, 5, null),
            new LoteDisponible(4, 5, null)
        };

        var consumos = AsignadorFefo.Asignar(lotes, 6, Hoy);

        Assert.Equal([new ConsumoDeLote(4, 5), new ConsumoDeLote(9, 1)], consumos);
    }

    [Fact]
    public void Lanza_si_el_stock_no_alcanza()
    {
        var lotes = new[]
        {
            new LoteDisponible(1, 2, Hoy.AddDays(5)),
            new LoteDisponible(2, 2, null)
        };

        Assert.Throws<StockInsuficienteException>(() => AsignadorFefo.Asignar(lotes, 5, Hoy));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Lanza_si_la_cantidad_es_cero_o_negativa(int cantidad)
    {
        var lotes = new[] { new LoteDisponible(1, 10, null) };

        Assert.Throws<ArgumentOutOfRangeException>(() => AsignadorFefo.Asignar(lotes, cantidad, Hoy));
    }

    [Fact]
    public void Una_lista_vacia_lanza_stock_insuficiente()
    {
        Assert.Throws<StockInsuficienteException>(() => AsignadorFefo.Asignar([], 1, Hoy));
    }
}
