using Proyecto_Arquitectura_Micromercado.Application.Facades;
using Proyecto_Arquitectura_Micromercado.Application.Sales;
using Proyecto_Arquitectura_Micromercado.Domain.Stock;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.EnMemoria;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Web;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class FachadaVentaTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 6, 15, 10, 0, 0, TimeSpan.Zero);

    private sealed class Escenario
    {
        public UnidadDeTrabajoEspia Unidad { get; } = new();
        public VentaRepositoryEnMemoria Ventas { get; } = new();
        public InMemoryLoteRepository Lotes { get; } = new();
        public DetectorDeInterbloqueoDoble Interbloqueos { get; } = new();
        public FachadaVenta Fachada { get; }

        public Escenario()
        {
            var reloj = new RelojFijo(Ahora);
            var stock = new FachadaActualizacionStockProducto(Lotes, reloj, new UnidadDeTrabajoEspia(), new UsuarioActualEnMemoria());
            var precios = new PrecioVigenteDoble(new Dictionary<int, decimal> { [1] = 10m, [2] = 5m });
            Fachada = new FachadaVenta(
                Unidad, Ventas, precios, stock, new UsuarioActualEnMemoria(id: 7), reloj, Interbloqueos);
        }

        public async Task<int> Disponible(int idProducto, int idLote)
        {
            var vigentes = await Lotes.ObtenerVigentesParaDescuentoAsync(idProducto);
            return vigentes.FirstOrDefault(l => l.IdLote == idLote)?.CantidadDisponible ?? 0;
        }
    }

    [Fact]
    public async Task Las_lineas_se_procesan_ordenadas_por_producto_sin_importar_el_orden_recibido()
    {
        var e = new Escenario();
        e.Lotes.Agregar(1, new LoteDisponible(10, 20, null));
        e.Lotes.Agregar(2, new LoteDisponible(20, 20, null));

        await e.Fachada.RegistrarVentaAsync(
            Venta(100m, new LineaVentaDto(2, 1), new LineaVentaDto(1, 1)));

        var detalles = Assert.Single(e.Ventas.Cabeceras).Detalles;
        Assert.Equal([1, 2], detalles.Select(d => d.IdProducto));
    }

    [Fact]
    public async Task Un_interbloqueo_reintenta_la_venta_completa_una_vez_sin_duplicar_nada()
    {
        var e = new Escenario();
        e.Lotes.Agregar(1, new LoteDisponible(10, 20, null));
        e.Ventas.FallosPendientes.Enqueue(new InterbloqueoSimulado());
        e.Interbloqueos.Reconocer<InterbloqueoSimulado>();

        var resultado = await e.Fachada.RegistrarVentaAsync(Venta(100m, new LineaVentaDto(1, 4)));

        Assert.Equal(2, e.Unidad.Iniciadas);
        Assert.Equal(1, e.Unidad.Revertidas);
        Assert.Equal(1, e.Unidad.Confirmadas);
        var cabecera = Assert.Single(e.Ventas.Cabeceras, c => c.NroComprobante is not null);
        Assert.Equal(resultado.IdVenta, cabecera.Id);
        Assert.Equal(4, Assert.Single(cabecera.Detalles).Cantidad);
    }

    [Fact]
    public async Task Un_segundo_interbloqueo_seguido_se_propaga_tras_un_solo_reintento()
    {
        var e = new Escenario();
        e.Lotes.Agregar(1, new LoteDisponible(10, 20, null));
        e.Ventas.FallosPendientes.Enqueue(new InterbloqueoSimulado());
        e.Ventas.FallosPendientes.Enqueue(new InterbloqueoSimulado());
        e.Interbloqueos.Reconocer<InterbloqueoSimulado>();

        await Assert.ThrowsAsync<InterbloqueoSimulado>(
            () => e.Fachada.RegistrarVentaAsync(Venta(100m, new LineaVentaDto(1, 4))));

        Assert.Equal(2, e.Unidad.Iniciadas);
        Assert.Equal(2, e.Unidad.Revertidas);
        Assert.Equal(0, e.Unidad.Confirmadas);
    }

    [Fact]
    public async Task Un_error_que_no_es_interbloqueo_no_se_reintenta()
    {
        var e = new Escenario();
        e.Lotes.Agregar(1, new LoteDisponible(10, 20, null));
        e.Ventas.FallosPendientes.Enqueue(new InterbloqueoSimulado());

        await Assert.ThrowsAsync<InterbloqueoSimulado>(
            () => e.Fachada.RegistrarVentaAsync(Venta(100m, new LineaVentaDto(1, 4))));

        Assert.Equal(1, e.Unidad.Iniciadas);
    }

    private static NuevaVentaDto Venta(decimal efectivo, params LineaVentaDto[] lineas) =>
        new(3, efectivo, lineas);

    [Fact]
    public async Task Una_venta_correcta_registra_cabecera_detalle_y_confirma()
    {
        var e = new Escenario();
        e.Lotes.Agregar(1, new LoteDisponible(10, 20, null));

        var resultado = await e.Fachada.RegistrarVentaAsync(Venta(100m, new LineaVentaDto(1, 4)));

        var cabecera = Assert.Single(e.Ventas.Cabeceras);
        Assert.Equal(resultado.IdVenta, cabecera.Id);
        Assert.Equal(3, cabecera.IdCliente);
        Assert.Equal(7, cabecera.IdUsuario);
        var detalle = Assert.Single(cabecera.Detalles);
        Assert.Equal(10, detalle.IdLote);
        Assert.Equal(4, detalle.Cantidad);
        Assert.Equal(10m, detalle.PrecioUnitario);
        Assert.Equal(40m, resultado.Total);
        Assert.Equal(40m, cabecera.MontoTotal);
        Assert.Equal(100m, cabecera.EfectivoPagado);
        Assert.Equal("V-20260615-0001", cabecera.NroComprobante);
        Assert.Equal(16, await e.Disponible(1, 10));
        Assert.Equal(1, e.Unidad.Iniciadas);
        Assert.Equal(1, e.Unidad.Confirmadas);
        Assert.Equal(0, e.Unidad.Revertidas);
    }

    [Fact]
    public async Task El_reparto_entre_dos_lotes_genera_dos_detalles()
    {
        var e = new Escenario();
        e.Lotes.Agregar(1, new LoteDisponible(10, 3, new DateOnly(2026, 7, 1)));
        e.Lotes.Agregar(1, new LoteDisponible(11, 10, new DateOnly(2026, 8, 1)));

        var resultado = await e.Fachada.RegistrarVentaAsync(Venta(100m, new LineaVentaDto(1, 5)));

        var detalles = Assert.Single(e.Ventas.Cabeceras).Detalles;
        Assert.Equal(2, detalles.Count);
        Assert.Equal((10, 3), (detalles[0].IdLote, detalles[0].Cantidad));
        Assert.Equal((11, 2), (detalles[1].IdLote, detalles[1].Cantidad));
        Assert.Equal(50m, resultado.Total);
    }

    [Fact]
    public async Task Stock_insuficiente_lanza_y_revierte_sin_confirmar()
    {
        var e = new Escenario();
        e.Lotes.Agregar(1, new LoteDisponible(10, 2, null));

        await Assert.ThrowsAsync<StockInsuficienteException>(
            () => e.Fachada.RegistrarVentaAsync(Venta(100m, new LineaVentaDto(1, 5))));

        Assert.Equal(1, e.Unidad.Iniciadas);
        Assert.Equal(0, e.Unidad.Confirmadas);
        Assert.Equal(1, e.Unidad.Revertidas);
    }

    [Fact]
    public async Task El_stock_insuficiente_en_un_producto_nombra_el_producto_y_su_presentacion_y_revierte_todo()
    {
        var e = new Escenario();
        e.Lotes.Agregar(1, new LoteDisponible(10, 20, null));
        e.Lotes.Agregar(2, new LoteDisponible(20, 1, null));

        var ex = await Assert.ThrowsAsync<StockInsuficienteException>(
            () => e.Fachada.RegistrarVentaAsync(Venta(500m, new LineaVentaDto(1, 2), new LineaVentaDto(2, 5))));

        Assert.Equal("Stock insuficiente para Yogurt Frutado PIL (Frasco 1kg).", ex.Message);
        Assert.Equal(0, e.Unidad.Confirmadas);
        Assert.Equal(1, e.Unidad.Revertidas);
    }

    [Fact]
    public async Task Efectivo_menor_al_total_lanza_y_revierte()
    {
        var e = new Escenario();
        e.Lotes.Agregar(1, new LoteDisponible(10, 20, null));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => e.Fachada.RegistrarVentaAsync(Venta(39m, new LineaVentaDto(1, 4))));

        Assert.Equal("El efectivo pagado es menor al monto total.", ex.Message);
        Assert.Equal(0, e.Unidad.Confirmadas);
        Assert.Equal(1, e.Unidad.Revertidas);
    }

    [Fact]
    public async Task Producto_inexistente_lanza_y_revierte()
    {
        var e = new Escenario();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => e.Fachada.RegistrarVentaAsync(Venta(100m, new LineaVentaDto(99, 1))));

        Assert.Equal("Producto inexistente o inactivo.", ex.Message);
        Assert.Equal(0, e.Unidad.Confirmadas);
        Assert.Equal(1, e.Unidad.Revertidas);
    }

    [Fact]
    public async Task Una_venta_con_dos_productos_suma_los_totales()
    {
        var e = new Escenario();
        e.Lotes.Agregar(1, new LoteDisponible(10, 20, null));
        e.Lotes.Agregar(2, new LoteDisponible(20, 20, null));

        var resultado = await e.Fachada.RegistrarVentaAsync(
            Venta(100m, new LineaVentaDto(1, 2), new LineaVentaDto(2, 3)));

        Assert.Equal(35m, resultado.Total);
        Assert.Equal(2, Assert.Single(e.Ventas.Cabeceras).Detalles.Count);
    }

    [Fact]
    public async Task Sin_cliente_lanza_sin_iniciar_transaccion()
    {
        var e = new Escenario();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => e.Fachada.RegistrarVentaAsync(new NuevaVentaDto(0, 10m, [new LineaVentaDto(1, 1)])));

        Assert.Equal(0, e.Unidad.Iniciadas);
    }

    [Fact]
    public async Task Sin_lineas_lanza_sin_iniciar_transaccion()
    {
        var e = new Escenario();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => e.Fachada.RegistrarVentaAsync(Venta(10m)));

        Assert.Equal(0, e.Unidad.Iniciadas);
    }

    [Fact]
    public async Task Cantidad_cero_lanza_sin_iniciar_transaccion()
    {
        var e = new Escenario();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => e.Fachada.RegistrarVentaAsync(Venta(10m, new LineaVentaDto(1, 0))));

        Assert.Equal(0, e.Unidad.Iniciadas);
    }

    [Fact]
    public async Task Efectivo_negativo_lanza_sin_iniciar_transaccion()
    {
        var e = new Escenario();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => e.Fachada.RegistrarVentaAsync(Venta(-1m, new LineaVentaDto(1, 1))));

        Assert.Equal(0, e.Unidad.Iniciadas);
    }
}
