using Proyecto_Arquitectura_Micromercado.Application.Facades;
using Proyecto_Arquitectura_Micromercado.Application.Sales;
using Proyecto_Arquitectura_Micromercado.Domain.Sales;
using Proyecto_Arquitectura_Micromercado.Domain.Stock;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.EnMemoria;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Web;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class FachadaAnulacionTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 6, 15, 10, 0, 0, TimeSpan.Zero);

    private sealed class Escenario
    {
        public UnidadDeTrabajoEspia Unidad { get; } = new();
        public VentaRepositoryEnMemoria Ventas { get; } = new();
        public InMemoryLoteRepository Lotes { get; } = new();
        public FachadaVenta Venta { get; }
        public FachadaAnulacion Anulacion { get; }

        public Escenario()
        {
            var reloj = new RelojFijo(Ahora);
            var stock = new FachadaActualizacionStockProducto(Lotes, reloj, new UnidadDeTrabajoEspia(), new UsuarioActualEnMemoria());
            var precios = new PrecioVigenteDoble(new Dictionary<int, decimal> { [1] = 10m });
            Venta = new FachadaVenta(
                new UnidadDeTrabajoEspia(), Ventas, precios, stock, new UsuarioActualEnMemoria(id: 7), reloj, new DetectorDeInterbloqueoDoble(), new ClientesDoble(3));
            Anulacion = new FachadaAnulacion(Unidad, Ventas, stock, new UsuarioActualEnMemoria(id: 9));
        }

        public async Task<int> RegistrarVentaDeCinco()
        {
            Lotes.Agregar(1, new LoteDisponible(10, 3, new DateOnly(2026, 7, 1)));
            Lotes.Agregar(1, new LoteDisponible(11, 10, new DateOnly(2026, 8, 1)));
            var resultado = await Venta.RegistrarVentaAsync(new NuevaVentaDto(3, [new LineaVentaDto(1, 5)]));
            return resultado.IdVenta;
        }

        public async Task<int> Disponible(int idLote)
        {
            var vigentes = await Lotes.ObtenerVigentesParaDescuentoAsync(1);
            return vigentes.FirstOrDefault(l => l.IdLote == idLote)?.CantidadDisponible ?? 0;
        }
    }

    [Fact]
    public async Task Una_anulacion_correcta_restaura_lotes_y_marca_la_venta()
    {
        var e = new Escenario();
        var idVenta = await e.RegistrarVentaDeCinco();
        Assert.Equal(0, await e.Disponible(10));
        Assert.Equal(8, await e.Disponible(11));

        await e.Anulacion.AnularVentaAsync(idVenta, "  Error de digitación  ");

        Assert.Equal(3, await e.Disponible(10));
        Assert.Equal(10, await e.Disponible(11));
        var cabecera = Assert.Single(e.Ventas.Cabeceras);
        Assert.Equal(EstadoVenta.Anulada, cabecera.Estado);
        Assert.Equal("Error de digitación", cabecera.Motivo);
        Assert.Equal(9, cabecera.IdUsuarioAnulacion);
        Assert.Equal(2, cabecera.Detalles.Count);
        Assert.Equal(1, e.Unidad.Iniciadas);
        Assert.Equal(1, e.Unidad.Confirmadas);
        Assert.Equal(0, e.Unidad.Revertidas);
    }

    [Fact]
    public async Task Una_venta_ya_anulada_lanza_revierte_y_no_cambia_el_stock()
    {
        var e = new Escenario();
        var idVenta = await e.RegistrarVentaDeCinco();
        await e.Anulacion.AnularVentaAsync(idVenta, "Primera anulación");
        var stockLote10 = await e.Disponible(10);
        var stockLote11 = await e.Disponible(11);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => e.Anulacion.AnularVentaAsync(idVenta, "Segunda anulación"));

        Assert.Equal("La venta no existe o ya fue anulada.", ex.Message);
        Assert.Equal(stockLote10, await e.Disponible(10));
        Assert.Equal(stockLote11, await e.Disponible(11));
        Assert.Equal(2, e.Unidad.Iniciadas);
        Assert.Equal(1, e.Unidad.Confirmadas);
        Assert.Equal(1, e.Unidad.Revertidas);
    }

    [Fact]
    public async Task Una_venta_inexistente_lanza_y_revierte()
    {
        var e = new Escenario();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => e.Anulacion.AnularVentaAsync(99, "Motivo"));

        Assert.Equal("La venta no existe o ya fue anulada.", ex.Message);
        Assert.Equal(1, e.Unidad.Iniciadas);
        Assert.Equal(0, e.Unidad.Confirmadas);
        Assert.Equal(1, e.Unidad.Revertidas);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Un_motivo_vacio_lanza_sin_iniciar_transaccion(string motivo)
    {
        var e = new Escenario();
        var idVenta = await e.RegistrarVentaDeCinco();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => e.Anulacion.AnularVentaAsync(idVenta, motivo));

        Assert.Equal(0, e.Unidad.Iniciadas);
    }

    [Fact]
    public async Task Un_id_de_venta_invalido_lanza_sin_iniciar_transaccion()
    {
        var e = new Escenario();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => e.Anulacion.AnularVentaAsync(0, "Motivo"));

        Assert.Equal(0, e.Unidad.Iniciadas);
    }

    [Fact]
    public async Task Si_restaurar_supera_la_cantidad_inicial_lanza_el_mensaje_claro_y_revierte()
    {
        var e = new Escenario();
        e.Lotes.Agregar(1, new LoteDisponible(10, 5, new DateOnly(2026, 10, 20)), cantidadInicial: 70);
        e.Lotes.Agregar(1, new LoteDisponible(11, 48, new DateOnly(2026, 11, 27)), cantidadInicial: 50);
        var idVenta = await e.Ventas.InsertarCabeceraAsync(3, 7);
        await e.Ventas.InsertarDetalleAsync(idVenta, 1, 10, 68, 6.5m);
        await e.Ventas.InsertarDetalleAsync(idVenta, 1, 11, 2, 6.5m);

        var ex = await Assert.ThrowsAsync<RestauracionExcedeCantidadInicialException>(
            () => e.Anulacion.AnularVentaAsync(idVenta, "Prueba"));

        Assert.Equal(
            "No se puede anular la venta: restaurar el stock de un lote superaría su cantidad inicial.",
            ex.Message);
        Assert.Equal(5, await e.Disponible(10));
        Assert.Equal(EstadoVenta.Completada, Assert.Single(e.Ventas.Cabeceras).Estado);
        Assert.Equal(0, e.Unidad.Confirmadas);
        Assert.Equal(1, e.Unidad.Revertidas);
    }
}
