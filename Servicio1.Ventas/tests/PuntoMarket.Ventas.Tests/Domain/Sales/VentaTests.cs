using PuntoMarket.Ventas.Domain.Sales;

namespace PuntoMarket.Ventas.Tests.Domain.Sales;

// Caracterización del comportamiento original; no agrega reglas al dominio.
public class VentaTests
{
    [Fact]
    public void Una_venta_nueva_esta_completada_sin_detalles_y_con_total_cero()
    {
        var venta = new Venta();

        Assert.Equal(EstadoVenta.Completada, venta.Estado);
        Assert.Equal("COMPLETADA", EstadoVenta.Completada);
        Assert.Equal("ANULADA", EstadoVenta.Anulada);
        Assert.Empty(venta.Detalles);
        Assert.Equal(0m, venta.Total);
    }

    [Fact]
    public void Agregar_detalles_conserva_producto_cantidad_precio_y_calcula_total()
    {
        var venta = new Venta();

        venta.AgregarDetalle(14, 3, 4.50m);
        venta.AgregarDetalle(20, 2, 1.25m);

        Assert.Collection(venta.Detalles,
            detalle =>
            {
                Assert.Equal(14, detalle.IdProducto);
                Assert.Equal(3, detalle.Cantidad);
                Assert.Equal(4.50m, detalle.PrecioUnitario);
                Assert.Equal(13.50m, detalle.Subtotal);
            },
            detalle => Assert.Equal(2.50m, detalle.Subtotal));
        Assert.Equal(16m, venta.Total);
    }

    [Theory]
    [InlineData(50, 50)]
    [InlineData(0, 13.5)]
    [InlineData(-1, 13.5)]
    public void El_total_asignado_solo_prevalece_si_es_positivo(double asignado, double esperado)
    {
        var venta = new Venta();
        venta.AgregarDetalle(14, 3, 4.50m);

        venta.Total = (decimal)asignado;

        Assert.Equal((decimal)esperado, venta.Total);
    }

    [Theory]
    [InlineData("ANULADA")]
    [InlineData("anulada")]
    [InlineData("AnUlAdA")]
    public void Una_venta_anulada_rechaza_detalles_sin_modificar_la_lista(string estado)
    {
        var venta = new Venta { Estado = estado };

        var error = Assert.Throws<InvalidOperationException>(() => venta.AgregarDetalle(14, 1, 4.50m));

        Assert.Equal("No se pueden agregar detalles a una venta anulada.", error.Message);
        Assert.Empty(venta.Detalles);
    }

    [Fact]
    public void Completar_sin_detalles_es_invalido()
    {
        var venta = new Venta();

        var error = Assert.Throws<InvalidOperationException>(() => venta.CompletarVenta());

        Assert.Equal("No se puede completar una venta sin detalles.", error.Message);
    }

    [Fact]
    public void Completar_con_detalles_establece_el_estado_completada()
    {
        var venta = new Venta();
        venta.AgregarDetalle(14, 1, 4.50m);

        venta.CompletarVenta();

        Assert.Equal(EstadoVenta.Completada, venta.Estado);
        Assert.Single(venta.Detalles);
        Assert.Equal(4.50m, venta.Total);
    }

    [Theory]
    [InlineData(7, "V-20260615-0007")]
    [InlineData(12345, "V-20260615-12345")]
    public void El_comprobante_conserva_fecha_y_relleno_minimo_del_identificador(int id, string esperado)
    {
        Assert.Equal(esperado, Venta.GenerarNroComprobante(new DateTime(2026, 6, 15, 10, 30, 0), id));
    }

    [Fact]
    public void El_subtotal_se_recalcula_al_cambiar_cantidad_o_precio()
    {
        var detalle = new DetalleVenta { Cantidad = 3, PrecioUnitario = 4.50m };
        Assert.Equal(13.50m, detalle.Subtotal);

        detalle.Cantidad = 2;
        detalle.PrecioUnitario = 1.25m;

        Assert.Equal(2.50m, detalle.Subtotal);
    }
}
