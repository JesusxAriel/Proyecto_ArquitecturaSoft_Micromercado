using Proyecto_Arquitectura_Micromercado.Application.Facades;
using Proyecto_Arquitectura_Micromercado.Domain.Stock;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.EnMemoria;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class FachadaActualizacionStockProductoTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 6, 15, 10, 0, 0, TimeSpan.Zero);

    private static (FachadaActualizacionStockProducto fachada, InMemoryLoteRepository lotes) Crear()
    {
        var lotes = new InMemoryLoteRepository();
        return (new FachadaActualizacionStockProducto(lotes, new RelojFijo(Ahora)), lotes);
    }

    private static async Task<int> Disponible(InMemoryLoteRepository lotes, int idLote)
    {
        var vigentes = await lotes.ObtenerVigentesParaDescuentoAsync(1);
        return vigentes.FirstOrDefault(l => l.IdLote == idLote)?.CantidadDisponible ?? 0;
    }

    [Fact]
    public async Task Descuenta_en_orden_FEFO_repartiendo_entre_lotes()
    {
        var (fachada, lotes) = Crear();
        lotes.Agregar(1, new LoteDisponible(1, 5, new DateOnly(2026, 9, 1)));
        lotes.Agregar(1, new LoteDisponible(2, 4, new DateOnly(2026, 7, 1)));

        var consumos = await fachada.DescontarAsync(1, 6);

        Assert.Equal([new ConsumoDeLote(2, 4), new ConsumoDeLote(1, 2)], consumos);
        Assert.Equal(0, await Disponible(lotes, 2));
        Assert.Equal(3, await Disponible(lotes, 1));
    }

    [Fact]
    public async Task Stock_insuficiente_lanza_StockInsuficienteException()
    {
        var (fachada, lotes) = Crear();
        lotes.Agregar(1, new LoteDisponible(1, 2, null));

        await Assert.ThrowsAsync<StockInsuficienteException>(() => fachada.DescontarAsync(1, 5));

        Assert.Equal(2, await Disponible(lotes, 1));
    }

    [Fact]
    public async Task Restaurar_devuelve_las_cantidades_a_cada_lote()
    {
        var (fachada, lotes) = Crear();
        lotes.Agregar(1, new LoteDisponible(1, 1, null));
        lotes.Agregar(1, new LoteDisponible(2, 1, null));

        await fachada.RestaurarAsync([new ConsumoDeLote(1, 3), new ConsumoDeLote(2, 4)]);

        Assert.Equal(4, await Disponible(lotes, 1));
        Assert.Equal(5, await Disponible(lotes, 2));
    }
}
