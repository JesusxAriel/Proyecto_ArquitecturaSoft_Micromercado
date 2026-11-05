using PuntoMarket.Ventas.Application.Sales;

namespace PuntoMarket.Ventas.Tests;

public partial class VentaConsultaServiceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task El_historial_conserva_resultados_filtro_limite_y_token(bool incluirAnuladas)
    {
        using var cancelacion = new CancellationTokenSource();
        VentaResumenDto[] ventas = [new(5, "V-5", new DateTime(2026, 6, 15), "Cliente", "COMPLETADA", 50m)];
        var puerto = new ConsultaRepositoryDoble { Ventas = ventas };

        var resultado = await Crear(puerto).ListarAsync(incluirAnuladas, cancelacion.Token);

        Assert.Equal(ventas, resultado);
        Assert.Equal(incluirAnuladas, puerto.IncluirAnuladasRecibido);
        Assert.Equal(50, puerto.LimiteRecibido);
        Assert.Equal(cancelacion.Token, puerto.TokenRecibido);
    }

    [Fact]
    public async Task Cada_lectura_consulta_el_puerto_y_observa_los_cambios_sin_cache()
    {
        var puerto = new ConsultaRepositoryDoble();
        var servicio = Crear(puerto);
        Assert.Empty(await servicio.ListarAsync(false));
        puerto.Ventas = [new(5, "V-5", new DateTime(2026, 6, 15), "Cliente", "COMPLETADA", 50m)];

        Assert.Single(await servicio.ListarAsync(false));
        Assert.Equal(2, puerto.LecturasHistorial);
    }

    [Fact]
    public async Task El_detalle_conserva_auditoria_y_separa_precios_del_mismo_producto()
    {
        using var cancelacion = new CancellationTokenSource();
        var fechaAnulacion = new DateTime(2026, 6, 16, 12, 0, 0);
        var venta = Venta(new(1, "Leche", 2, 10m, 20m, "LOT-A"), new(1, "Leche", 1, 12m, 12m, "LOT-B"))
            with { Estado = "ANULADA", MotivoAnulacion = "Error", FechaAnulacion = fechaAnulacion, UsuarioAnulacion = "Admin" };
        var puerto = new ConsultaRepositoryDoble { Detalle = venta };

        var detalle = await Crear(puerto).ObtenerDetalleAsync(5, cancelacion.Token);

        Assert.NotNull(detalle);
        Assert.Equal(venta.Id, detalle.Id);
        Assert.Equal(venta.NroComprobante, detalle.NroComprobante);
        Assert.Equal(venta.FechaHora, detalle.FechaHora);
        Assert.Equal(venta.NitCiCliente, detalle.NitCiCliente);
        Assert.Equal(venta.RazonSocialCliente, detalle.RazonSocialCliente);
        Assert.Equal(venta.UsuarioCajero, detalle.UsuarioCajero);
        Assert.Equal(venta.Total, detalle.Total);
        Assert.Equal("ANULADA", detalle.Estado);
        Assert.Equal("Error", detalle.MotivoAnulacion);
        Assert.Equal(fechaAnulacion, detalle.FechaAnulacion);
        Assert.Equal("Admin", detalle.UsuarioAnulacion);
        Assert.Equal([10m, 12m], detalle.Lineas.Select(l => l.PrecioUnitario));
        Assert.Equal([20m, 12m], detalle.Lineas.Select(l => l.Subtotal));
        Assert.Equal(cancelacion.Token, puerto.TokenRecibido);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Un_id_invalido_no_consulta_el_repositorio(int id)
    {
        var puerto = new ConsultaRepositoryDoble();

        Assert.Null(await Crear(puerto).ObtenerDetalleAsync(id));
        Assert.Equal(0, puerto.LecturasDetalle);
    }

    [Fact]
    public async Task Las_ventas_del_dia_se_obtienen_del_puerto_con_el_token_original()
    {
        using var cancelacion = new CancellationTokenSource();
        var ventas = new VentaRepositoryEnMemoria { VentasDelDia = 123.45m };
        var servicio = new VentaConsultaService(ventas, new ConsultaRepositoryDoble());

        Assert.Equal(123.45m, await servicio.ObtenerVentasDelDiaAsync(cancelacion.Token));
        Assert.Equal(cancelacion.Token, ventas.TokenConsultaDelDia);
    }

    [Fact]
    public async Task El_error_del_puerto_de_consultas_se_propaga()
    {
        var error = new InvalidOperationException("Error de lectura");
        var servicio = Crear(new ConsultaRepositoryDoble { ErrorLectura = error });

        Assert.Same(error, await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.ListarAsync(false)));
        Assert.Same(error, await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.ObtenerDetalleAsync(5)));
    }

    [Fact]
    public async Task La_cancelacion_se_transmite_al_puerto()
    {
        using var cancelacion = new CancellationTokenSource();
        cancelacion.Cancel();
        var servicio = Crear(new ConsultaRepositoryDoble());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => servicio.ListarAsync(false, cancelacion.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => servicio.ObtenerDetalleAsync(5, cancelacion.Token));
    }
}
