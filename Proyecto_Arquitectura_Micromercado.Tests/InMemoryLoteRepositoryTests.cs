using Proyecto_Arquitectura_Micromercado.Domain.Stock;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.EnMemoria;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class InMemoryLoteRepositoryTests
{
    private static InMemoryLoteRepository RepositorioConLote(int cantidad)
    {
        var repositorio = new InMemoryLoteRepository();
        repositorio.Agregar(1, new LoteDisponible(10, cantidad, null));
        return repositorio;
    }

    [Fact]
    public async Task Descontar_resta_la_cantidad_del_lote()
    {
        var repositorio = RepositorioConLote(10);

        await repositorio.DescontarAsync(10, 4);

        var vigentes = await repositorio.ObtenerVigentesParaDescuentoAsync(1);
        Assert.Equal(6, Assert.Single(vigentes).CantidadDisponible);
    }

    [Fact]
    public async Task Descontar_de_mas_lanza_stock_insuficiente()
    {
        var repositorio = RepositorioConLote(3);

        await Assert.ThrowsAsync<StockInsuficienteException>(() => repositorio.DescontarAsync(10, 4));

        var vigentes = await repositorio.ObtenerVigentesParaDescuentoAsync(1);
        Assert.Equal(3, Assert.Single(vigentes).CantidadDisponible);
    }

    [Fact]
    public async Task Descontar_de_un_lote_inexistente_lanza_stock_insuficiente()
    {
        var repositorio = RepositorioConLote(3);

        await Assert.ThrowsAsync<StockInsuficienteException>(() => repositorio.DescontarAsync(99, 1));
    }

    [Fact]
    public async Task Restaurar_suma_la_cantidad_al_lote()
    {
        var repositorio = RepositorioConLote(5);

        await repositorio.RestaurarAsync(10, 2);

        var vigentes = await repositorio.ObtenerVigentesParaDescuentoAsync(1);
        Assert.Equal(7, Assert.Single(vigentes).CantidadDisponible);
    }

    [Fact]
    public async Task ObtenerVigentes_excluye_cantidad_cero_y_lotes_de_otro_producto()
    {
        var repositorio = new InMemoryLoteRepository();
        repositorio.Agregar(1, new LoteDisponible(10, 5, null));
        repositorio.Agregar(1, new LoteDisponible(11, 0, null));
        repositorio.Agregar(2, new LoteDisponible(12, 8, null));

        var vigentes = await repositorio.ObtenerVigentesParaDescuentoAsync(1);

        Assert.Equal(10, Assert.Single(vigentes).IdLote);
    }

    [Fact]
    public async Task Restaurar_que_supera_la_cantidad_inicial_lanza_y_no_cambia_el_lote()
    {
        var repositorio = new InMemoryLoteRepository();
        repositorio.Agregar(1, new LoteDisponible(10, 5, null), cantidadInicial: 6);

        await Assert.ThrowsAsync<RestauracionExcedeCantidadInicialException>(
            () => repositorio.RestaurarAsync(10, 2));

        var vigentes = await repositorio.ObtenerVigentesParaDescuentoAsync(1);
        Assert.Equal(5, Assert.Single(vigentes).CantidadDisponible);
    }

    [Fact]
    public async Task Restaurar_hasta_la_cantidad_inicial_es_valido()
    {
        var repositorio = new InMemoryLoteRepository();
        repositorio.Agregar(1, new LoteDisponible(10, 5, null), cantidadInicial: 7);

        await repositorio.RestaurarAsync(10, 2);

        var vigentes = await repositorio.ObtenerVigentesParaDescuentoAsync(1);
        Assert.Equal(7, Assert.Single(vigentes).CantidadDisponible);
    }
}
