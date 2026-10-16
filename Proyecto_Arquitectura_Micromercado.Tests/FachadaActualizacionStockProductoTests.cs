using Proyecto_Arquitectura_Micromercado.Application.Facades;
using Proyecto_Arquitectura_Micromercado.Application.Stock;
using Proyecto_Arquitectura_Micromercado.Domain.Stock;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.EnMemoria;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Web;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class FachadaActualizacionStockProductoTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 6, 15, 10, 0, 0, TimeSpan.Zero);

    private static (FachadaActualizacionStockProducto fachada, InMemoryLoteRepository lotes) Crear()
    {
        var lotes = new InMemoryLoteRepository();
        return (new FachadaActualizacionStockProducto(
            lotes, new RelojFijo(Ahora), new UnidadDeTrabajoEspia(), new UsuarioActualEnMemoria()), lotes);
    }

    private static (FachadaActualizacionStockProducto fachada, InMemoryLoteRepository lotes, UnidadDeTrabajoEspia unidad) CrearConUnidad(
        ILoteRepository? lotesPersonalizados = null)
    {
        var lotes = new InMemoryLoteRepository();
        var unidad = new UnidadDeTrabajoEspia();
        var fachada = new FachadaActualizacionStockProducto(
            lotesPersonalizados ?? lotes, new RelojFijo(Ahora), unidad, new UsuarioActualEnMemoria(id: 4));
        return (fachada, lotes, unidad);
    }

    private sealed class LoteRepositoryQueFalla : ILoteRepository
    {
        public Task<IReadOnlyList<LoteDisponible>> ObtenerVigentesParaDescuentoAsync(int idProducto, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task DescontarAsync(int idLote, int cantidad, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task RestaurarAsync(int idLote, int cantidad, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<LoteDisponible>> ObtenerVencidosConStockAsync(DateOnly hoy, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<LoteDisponible> lotes = [new LoteDisponible(10, 5, hoy.AddDays(-1))];
            return Task.FromResult(lotes);
        }

        public Task DesactivarAsync(int idLote, int idUsuario, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Fallo al desactivar.");
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

    [Fact]
    public async Task La_baja_desactiva_solo_los_lotes_vencidos_con_stock_y_registra_al_usuario()
    {
        var (fachada, lotes, unidad) = CrearConUnidad();
        lotes.Agregar(1, new LoteDisponible(10, 5, new DateOnly(2026, 6, 14)));
        lotes.Agregar(1, new LoteDisponible(11, 0, new DateOnly(2026, 6, 1)));
        lotes.Agregar(1, new LoteDisponible(12, 8, new DateOnly(2026, 6, 15)));
        lotes.Agregar(2, new LoteDisponible(13, 7, null));
        lotes.Agregar(2, new LoteDisponible(14, 3, new DateOnly(2026, 1, 1)));

        var resultado = await fachada.DarDeBajaLotesVencidosAsync();

        Assert.Equal(new ResultadoBajaDeLotes(2, 8), resultado);
        Assert.False(lotes.EstaActivo(10));
        Assert.False(lotes.EstaActivo(14));
        Assert.True(lotes.EstaActivo(11));
        Assert.True(lotes.EstaActivo(12));
        Assert.True(lotes.EstaActivo(13));
        Assert.Equal(4, lotes.IdUsuarioActualizacionDe(10));
        Assert.Equal(4, lotes.IdUsuarioActualizacionDe(14));
        Assert.Null(lotes.IdUsuarioActualizacionDe(12));
        Assert.Equal(1, unidad.Iniciadas);
        Assert.Equal(1, unidad.Confirmadas);
        Assert.Equal(0, unidad.Revertidas);
    }

    [Fact]
    public async Task Sin_lotes_vencidos_la_baja_confirma_sin_cambios()
    {
        var (fachada, lotes, unidad) = CrearConUnidad();
        lotes.Agregar(1, new LoteDisponible(10, 5, new DateOnly(2026, 7, 1)));

        var resultado = await fachada.DarDeBajaLotesVencidosAsync();

        Assert.Equal(new ResultadoBajaDeLotes(0, 0), resultado);
        Assert.True(lotes.EstaActivo(10));
        Assert.Equal(1, unidad.Confirmadas);
    }

    [Fact]
    public async Task Un_lote_dado_de_baja_deja_de_venderse()
    {
        var (fachada, lotes, _) = CrearConUnidad();
        lotes.Agregar(1, new LoteDisponible(10, 5, new DateOnly(2026, 6, 14)));
        lotes.Agregar(1, new LoteDisponible(11, 9, new DateOnly(2026, 9, 1)));

        await fachada.DarDeBajaLotesVencidosAsync();

        var consumos = await fachada.DescontarAsync(1, 4);
        Assert.Equal([new ConsumoDeLote(11, 4)], consumos);
        await Assert.ThrowsAsync<StockInsuficienteException>(() => fachada.DescontarAsync(1, 6));
    }

    [Fact]
    public async Task Si_la_baja_falla_revierte_la_transaccion()
    {
        var (fachada, _, unidad) = CrearConUnidad(new LoteRepositoryQueFalla());

        await Assert.ThrowsAsync<InvalidOperationException>(() => fachada.DarDeBajaLotesVencidosAsync());

        Assert.Equal(1, unidad.Iniciadas);
        Assert.Equal(0, unidad.Confirmadas);
        Assert.Equal(1, unidad.Revertidas);
    }
}
