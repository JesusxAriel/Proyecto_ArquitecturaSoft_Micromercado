using Proyecto_Arquitectura_Micromercado.Application.Comprobantes;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Comprobantes;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.EnMemoria;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Web;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class ComprobanteTests
{
    // Reloj que siempre marca la misma hora, para comprobar la fecha de generacion.
    private sealed class RelojFijo(DateTime momento) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(momento, TimeSpan.Zero);

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    private static readonly DateTime MomentoDeGeneracion = new(2030, 6, 23, 10, 15, 45);

    private static VentaParaComprobante Venta(
        string estado = EstadoDeVenta.Completada,
        params DetalleDeVenta[] detalle) =>
        new(
            IdVenta: 7,
            NroComprobante: "V-20300623-0007",
            FechaVenta: new DateTime(2030, 6, 23, 9, 58, 0),
            NitCiCliente: "4545451226",
            RazonSocialCliente: "JUAN PEREZ VARGAS",
            Estado: estado,
            MontoTotal: detalle.Sum(fila => fila.Subtotal),
            Detalle: detalle);

    private static ComprobanteService Servicio(
        VentaParaComprobante? venta,
        string usuario = "cajero1")
    {
        var repositorio = new InMemoryComprobanteRepository();

        if (venta is not null)
        {
            repositorio.Agregar(venta);
        }

        return new ComprobanteService(
            repositorio,
            new UsuarioActualEnMemoria(id: 2, nombreUsuario: usuario, rol: "Cajero"),
            new RelojFijo(MomentoDeGeneracion));
    }

    private static DetalleDeVenta Fila(int idProducto, string nombre, int cantidad, decimal precio) =>
        new(idProducto, nombre, cantidad, precio, cantidad * precio);

    // ---- Servicio

    [Fact]
    public async Task Arma_el_comprobante_con_los_datos_de_la_venta_la_hora_actual_y_el_usuario_actual()
    {
        var venta = Venta(
            detalle:
            [
                Fila(14, "Galletas Moka Arcor 110g", 1, 4.50m),
                Fila(10, "Salsa de Tomate Kris 400g", 2, 8.00m)
            ]);

        var comprobante = await Servicio(venta, usuario: "cajero1").ObtenerAsync(7);

        Assert.NotNull(comprobante);
        Assert.Equal("V-20300623-0007", comprobante.NroComprobante);
        Assert.Equal("4545451226", comprobante.NitCiCliente);
        Assert.Equal("JUAN PEREZ VARGAS", comprobante.RazonSocialCliente);
        Assert.Equal(new DateTime(2030, 6, 23, 9, 58, 0), comprobante.FechaVenta);
        Assert.Equal(20.50m, comprobante.Total);
        Assert.Equal(MomentoDeGeneracion, comprobante.FechaGeneracion);
        Assert.Equal("cajero1", comprobante.UsuarioGenerador);
        Assert.False(comprobante.EstaAnulada);
    }

    [Fact]
    public async Task El_usuario_del_comprobante_es_quien_lo_genera_no_quien_registro_la_venta()
    {
        var venta = Venta(detalle: [Fila(14, "Galletas Moka Arcor 110g", 1, 4.50m)]);

        var comprobante = await Servicio(venta, usuario: "admin").ObtenerAsync(7);

        Assert.NotNull(comprobante);
        Assert.Equal("admin", comprobante.UsuarioGenerador);
    }

    [Fact]
    public async Task Dos_lotes_del_mismo_producto_se_agrupan_en_una_sola_linea()
    {
        // 100 unidades de leche salieron de dos lotes: 70 del que vence primero y 30 del
        // siguiente. El comprobante debe mostrar una sola linea de 100.
        var venta = Venta(
            detalle:
            [
                Fila(1, "Leche Entera PIL 1L", 70, 6.50m),
                Fila(2, "Yogurt Frutado PIL 1kg", 1, 12.00m),
                Fila(1, "Leche Entera PIL 1L", 30, 6.50m)
            ]);

        var comprobante = await Servicio(venta).ObtenerAsync(7);

        Assert.NotNull(comprobante);
        Assert.Equal(2, comprobante.Lineas.Count);

        var leche = comprobante.Lineas[0];
        Assert.Equal("Leche Entera PIL 1L", leche.Descripcion);
        Assert.Equal(100, leche.Cantidad);
        Assert.Equal(6.50m, leche.PrecioUnitario);
        Assert.Equal(650.00m, leche.Importe);

        var yogurt = comprobante.Lineas[1];
        Assert.Equal(1, yogurt.Cantidad);
        Assert.Equal(12.00m, yogurt.Importe);

        Assert.Equal(662.00m, comprobante.Total);
    }

    [Fact]
    public async Task Productos_distintos_con_el_mismo_nombre_no_se_mezclan()
    {
        var venta = Venta(
            detalle:
            [
                Fila(1, "Aceite 1L", 2, 10.00m),
                Fila(2, "Aceite 1L", 3, 10.00m)
            ]);

        var comprobante = await Servicio(venta).ObtenerAsync(7);

        Assert.NotNull(comprobante);
        Assert.Equal(2, comprobante.Lineas.Count);
    }

    [Fact]
    public async Task Una_venta_inexistente_devuelve_null_en_lugar_de_lanzar()
    {
        Assert.Null(await Servicio(venta: null).ObtenerAsync(99));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task Un_identificador_no_positivo_devuelve_null(int idVenta)
    {
        var venta = Venta(detalle: [Fila(1, "Leche", 1, 6.50m)]);

        Assert.Null(await Servicio(venta).ObtenerAsync(idVenta));
    }

    [Fact]
    public async Task Una_venta_anulada_se_marca_como_anulada()
    {
        var venta = Venta(EstadoDeVenta.Anulada, Fila(1, "Leche", 1, 6.50m));

        var comprobante = await Servicio(venta).ObtenerAsync(7);

        Assert.NotNull(comprobante);
        Assert.True(comprobante.EstaAnulada);
    }

    // ---- Generador HTML

    private static async Task<string> HtmlDe(VentaParaComprobante venta, string usuario = "cajero1")
    {
        var comprobante = await Servicio(venta, usuario).ObtenerAsync(7);
        Assert.NotNull(comprobante);

        return new GeneradorComprobanteHtml().GenerarHtml(comprobante);
    }

    [Fact]
    public async Task El_html_incluye_el_total_el_usuario_y_la_fecha_y_hora_de_generacion()
    {
        var html = await HtmlDe(
            Venta(detalle:
            [
                Fila(14, "Galletas Moka Arcor 110g", 1, 4.50m),
                Fila(10, "Salsa de Tomate Kris 400g", 2, 8.00m),
                Fila(20, "Tostadas Trigo PIL 200g", 1, 7.50m)
            ]),
            usuario: "cajero1");

        Assert.Contains("COMPROBANTE DE VENTA", html);
        Assert.Contains("28,00", html);
        Assert.Contains("cajero1", html);
        Assert.Contains("23/06/2030", html);
        Assert.Contains("10:15:45", html);
        Assert.Contains("JUAN PEREZ VARGAS", html);
        Assert.Contains("4545451226", html);
        Assert.Contains("Galletas Moka Arcor 110g", html);
        Assert.Contains("/images/logo.jpg", html);
        Assert.DoesNotContain("role=\"alert\"", html);
    }

    [Fact]
    public async Task El_html_de_una_venta_anulada_muestra_el_estado_anulada()
    {
        var html = await HtmlDe(Venta(EstadoDeVenta.Anulada, Fila(1, "Leche", 1, 6.50m)));

        Assert.Contains("role=\"alert\">ANULADA</p>", html);
    }

    [Fact]
    public async Task El_html_escapa_el_texto_que_viene_de_la_base_de_datos()
    {
        var venta = Venta(detalle: [Fila(1, "<b>Leche</b>", 1, 6.50m)]) with
        {
            RazonSocialCliente = "<script>alert('x')</script>"
        };

        var html = await HtmlDe(venta, usuario: "<i>admin</i>");

        Assert.DoesNotContain("<script>alert", html);
        Assert.DoesNotContain("<b>Leche</b>", html);
        Assert.DoesNotContain("<i>admin</i>", html);
        Assert.Contains("&lt;script&gt;", html);
    }

    [Fact]
    public async Task El_boton_imprimir_se_oculta_al_imprimir()
    {
        var html = await HtmlDe(Venta(detalle: [Fila(1, "Leche", 1, 6.50m)]));

        Assert.Contains("Imprimir", html);
        Assert.Contains("@media print", html);
        Assert.Contains(".acciones { display: none; }", html);
    }
}
