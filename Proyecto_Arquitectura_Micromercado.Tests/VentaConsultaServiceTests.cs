using Proyecto_Arquitectura_Micromercado.Application.Sales;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class VentaConsultaServiceTests
{
    private sealed class ConsultaRepositoryDoble : IVentaConsultaRepository
    {
        public bool? IncluirAnuladasRecibido { get; private set; }
        public int? LimiteRecibido { get; private set; }
        public VentaDetalleConFilasDto? Detalle { get; init; }

        public Task<IReadOnlyList<VentaResumenDto>> ListarAsync(
            bool incluirAnuladas, int limite, CancellationToken cancellationToken = default)
        {
            IncluirAnuladasRecibido = incluirAnuladas;
            LimiteRecibido = limite;
            return Task.FromResult<IReadOnlyList<VentaResumenDto>>([]);
        }

        public Task<VentaDetalleConFilasDto?> ObtenerDetalleAsync(
            int idVenta, CancellationToken cancellationToken = default) =>
            Task.FromResult(Detalle is not null && Detalle.Id == idVenta ? Detalle : null);
    }

    private static VentaDetalleConFilasDto Venta(params FilaDetalleVenta[] filas) => new(
        5, "V-20260615-0005", new DateTime(2026, 6, 15, 10, 30, 0), "123", "Cliente Uno",
        "Cajero Uno", "COMPLETADA", 100m, null, null, null, filas);

    private static VentaConsultaService Crear(ConsultaRepositoryDoble doble) =>
        new(new VentaRepositoryEnMemoria(), doble);

    [Fact]
    public async Task Agrupa_las_filas_del_mismo_producto_en_una_linea()
    {
        var servicio = Crear(new ConsultaRepositoryDoble
        {
            Detalle = Venta(
                new FilaDetalleVenta(1, "Leche", 3, 10m, 30m, "LOT-A"),
                new FilaDetalleVenta(1, "Leche", 2, 10m, 20m, "LOT-B"))
        });

        var detalle = await servicio.ObtenerDetalleAsync(5);

        var linea = Assert.Single(detalle!.Lineas);
        Assert.Equal(5, linea.Cantidad);
        Assert.Equal(50m, linea.Subtotal);
        Assert.Equal("Leche", linea.Descripcion);
        Assert.Equal(
            [new LoteConsumidoDto("LOT-A", 3), new LoteConsumidoDto("LOT-B", 2)],
            linea.Lotes);
    }

    [Fact]
    public async Task Las_filas_del_mismo_lote_se_suman_dentro_de_la_linea()
    {
        var servicio = Crear(new ConsultaRepositoryDoble
        {
            Detalle = Venta(
                new FilaDetalleVenta(1, "Leche", 3, 10m, 30m, "LOT-A"),
                new FilaDetalleVenta(1, "Leche", 4, 10m, 40m, "LOT-A"))
        });

        var detalle = await servicio.ObtenerDetalleAsync(5);

        var linea = Assert.Single(detalle!.Lineas);
        Assert.Equal([new LoteConsumidoDto("LOT-A", 7)], linea.Lotes);
    }

    [Fact]
    public async Task No_agrupa_productos_distintos()
    {
        var servicio = Crear(new ConsultaRepositoryDoble
        {
            Detalle = Venta(
                new FilaDetalleVenta(1, "Leche", 3, 10m, 30m, "LOT-A"),
                new FilaDetalleVenta(2, "Pan", 2, 5m, 10m, "LOT-P"))
        });

        var detalle = await servicio.ObtenerDetalleAsync(5);

        Assert.Equal(2, detalle!.Lineas.Count);
    }

    [Fact]
    public async Task ListarAsync_pasa_incluirAnuladas_y_pide_limite_50()
    {
        var doble = new ConsultaRepositoryDoble();
        var servicio = Crear(doble);

        await servicio.ListarAsync(true);

        Assert.True(doble.IncluirAnuladasRecibido);
        Assert.Equal(50, doble.LimiteRecibido);
    }

    [Fact]
    public async Task Un_detalle_inexistente_devuelve_null()
    {
        var servicio = Crear(new ConsultaRepositoryDoble { Detalle = Venta() });

        Assert.Null(await servicio.ObtenerDetalleAsync(99));
    }

    [Fact]
    public async Task Un_id_cero_devuelve_null()
    {
        var servicio = Crear(new ConsultaRepositoryDoble { Detalle = Venta() });

        Assert.Null(await servicio.ObtenerDetalleAsync(0));
    }
}
