using PuntoMarket.Ventas.Application.Customers;
using PuntoMarket.Ventas.Application.Products;
using PuntoMarket.Ventas.Application.Comprobantes;
using PuntoMarket.Ventas.Infrastructure.Persistence;

namespace PuntoMarket.Ventas.Tests;

public class ConsultasComplementariasTests
{
    private static ProductoVentaConsultaService Productos(ConsultasComplementariasDoble puerto) =>
        new(puerto, new RelojFijo(new DateTimeOffset(2026, 6, 15, 10, 0, 0, TimeSpan.Zero)));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("a")]
    public async Task Productos_con_termino_corto_no_consultan_el_puerto(string? termino)
    {
        var puerto = new ConsultasComplementariasDoble();
        var resultado = await Productos(puerto).BuscarAsync(termino);
        Assert.Empty(resultado.Productos);
        Assert.False(resultado.HayMas);
        Assert.Equal(0, puerto.Lecturas);
    }

    [Fact]
    public async Task La_busqueda_de_productos_normaliza_limita_y_pasa_el_dia_FEFO_y_token()
    {
        using var cancelacion = new CancellationTokenSource();
        var puerto = new ConsultasComplementariasDoble();
        var resultado = await Productos(puerto).BuscarAsync("  Leche  ", cancelacion.Token);
        Assert.Equal(1, Assert.Single(resultado.Productos).Id);
        Assert.Equal("Leche", puerto.Termino);
        Assert.Equal(10, puerto.Limite);
        Assert.Equal(new DateOnly(2026, 6, 15), puerto.Hoy);
        Assert.Equal(cancelacion.Token, puerto.Token);
    }

    [Fact]
    public async Task Mas_de_diez_productos_conserva_el_indicador_hayMas()
    {
        var puerto = new ConsultasComplementariasDoble();
        for (var id = 10; id < 22; id++) puerto.Productos.Add((new(id, "Leche " + id, "Unidad", 1m, 1), true));
        var resultado = await Productos(puerto).BuscarAsync("Leche");
        Assert.Equal(10, resultado.Total);
        Assert.True(resultado.HayMas);
    }

    [Fact]
    public async Task Las_consultas_conservan_precio_stock_cero_y_productos_no_disponibles()
    {
        var servicio = Productos(new ConsultasComplementariasDoble());
        var leche = await servicio.ObtenerAsync(1);
        Assert.NotNull(leche);
        Assert.Equal(10.50m, leche.PrecioVenta);
        Assert.Equal(8, leche.StockDisponible);
        Assert.Equal(0, (await servicio.ObtenerAsync(3))!.StockDisponible);
        Assert.Null(await servicio.ObtenerAsync(2));
        Assert.Null(await servicio.ObtenerAsync(99));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Un_id_invalido_no_consulta_productos(int id)
    {
        var puerto = new ConsultasComplementariasDoble();
        Assert.Null(await Productos(puerto).ObtenerAsync(id));
        Assert.Equal(0, puerto.Lecturas);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Clientes_sin_termino_no_consultan_el_puerto(string? termino)
    {
        var puerto = new ConsultasComplementariasDoble();
        Assert.Empty(await new ClienteVentaConsultaService(puerto).BuscarAsync(termino));
        Assert.Equal(0, puerto.Lecturas);
    }

    [Theory]
    [InlineData(" 123 ")]
    [InlineData(" Cliente ")]
    public async Task Clientes_busca_por_nombre_o_documento_y_limita_veinte(string termino)
    {
        using var cancelacion = new CancellationTokenSource();
        var puerto = new ConsultasComplementariasDoble();
        var resultado = await new ClienteVentaConsultaService(puerto).BuscarAsync(termino, cancelacion.Token);
        Assert.Equal(3, Assert.Single(resultado).Id);
        Assert.Equal(termino.Trim(), puerto.Termino);
        Assert.Equal(20, puerto.Limite);
        Assert.Equal(cancelacion.Token, puerto.Token);
    }

    [Fact]
    public async Task Filtros_largos_y_cancelacion_se_rechazan_antes_de_leer()
    {
        var puerto = new ConsultasComplementariasDoble();
        var productos = Productos(puerto);
        var clientes = new ClienteVentaConsultaService(puerto);
        await Assert.ThrowsAsync<ArgumentException>(() => productos.BuscarAsync(new string('a', 151)));
        await Assert.ThrowsAsync<ArgumentException>(() => clientes.BuscarAsync(new string('a', 151)));
        using var cancelacion = new CancellationTokenSource();
        cancelacion.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => productos.BuscarAsync("Leche", cancelacion.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => clientes.BuscarAsync("123", cancelacion.Token));
        Assert.Equal(0, puerto.Lecturas);
    }

    [Fact]
    public async Task Búsquedas_sin_coincidencias_devuelven_colecciones_vacias()
    {
        var puerto = new ConsultasComplementariasDoble();
        Assert.Empty((await Productos(puerto).BuscarAsync("no-existe")).Productos);
        Assert.Empty(await new ClienteVentaConsultaService(puerto).BuscarAsync("no-existe"));
    }

    [Fact]
    public async Task Comprobantes_separan_precios_y_propagan_cancelacion_sin_escribir()
    {
        var puerto = new ConsultasComplementariasDoble();
        puerto.Venta = puerto.Venta! with { Detalle = [new(1, "Leche", 1, 10m, 10m), new(1, "Leche", 1, 12m, 12m)] };
        var comprobante = await new ComprobanteService(puerto).ObtenerAsync(7);
        Assert.Equal(2, comprobante!.Lineas.Count);
        using var cancelacion = new CancellationTokenSource();
        cancelacion.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new ComprobanteService(puerto).ObtenerAsync(7, cancelacion.Token));
    }

    [Theory]
    [InlineData("leche", "%leche%")]
    [InlineData("10%_!", "%10!%!_!!%")]
    [InlineData("O'Reilly", "%O'Reilly%")]
    public void Los_metacaracteres_de_busqueda_se_tratan_como_literales(string termino, string esperado) =>
        Assert.Equal(esperado, PatronBusqueda.Contiene(termino));

    [Fact]
    public void El_SQL_del_catalogo_no_contiene_escrituras_ni_bloqueos()
    {
        foreach (var sql in new[] { MySqlProductoVentaConsultaRepository.BusquedaSql,
            MySqlProductoVentaConsultaRepository.DetalleSql, MySqlClienteVentaConsultaRepository.BusquedaSql })
            Assert.DoesNotMatch(@"(?i)\b(INSERT|UPDATE|DELETE|REPLACE|DROP|ALTER|CREATE|TRUNCATE|LOCK)\b", sql);
    }
}
