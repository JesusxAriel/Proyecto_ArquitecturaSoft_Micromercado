using Proyecto_Arquitectura_Micromercado.Application.Comprobantes;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Comprobantes;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.EnMemoria;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Web;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class ComprobanteTests
{
    private static readonly DateTime MomentoDeLaVenta = new(2030, 6, 23, 9, 58, 0);

    private static VentaParaComprobante Venta(
        string estado = EstadoDeVenta.Completada,
        params DetalleDeVenta[] detalle) =>
        new(
            IdVenta: 7,
            NroComprobante: "V-20300623-0007",
            FechaVenta: MomentoDeLaVenta,
            NitCiCliente: "4545451226",
            RazonSocialCliente: "JUAN PEREZ VARGAS",
            Estado: estado,
            MontoTotal: detalle.Sum(fila => fila.Subtotal),
            UsuarioCajero: "cajero1",
            Detalle: detalle);

    private static ComprobanteService Servicio(VentaParaComprobante? venta)
    {
        var repositorio = new InMemoryComprobanteRepository();

        if (venta is not null)
        {
            repositorio.Agregar(venta);
        }

        return new ComprobanteService(repositorio);
    }

    private static DetalleDeVenta Fila(int idProducto, string nombre, int cantidad, decimal precio) =>
        new(idProducto, nombre, cantidad, precio, cantidad * precio);

    [Fact]
    public async Task Arma_el_comprobante_con_los_datos_de_la_venta_su_fecha_y_el_cajero_que_la_registro()
    {
        var venta = Venta(
            detalle:
            [
                Fila(14, "Galletas Moka Arcor 110g", 1, 4.50m),
                Fila(10, "Salsa de Tomate Kris 400g", 2, 8.00m)
            ]);

        var comprobante = await Servicio(venta).ObtenerAsync(7);

        Assert.NotNull(comprobante);
        Assert.Equal("V-20300623-0007", comprobante.NroComprobante);
        Assert.Equal("4545451226", comprobante.NitCiCliente);
        Assert.Equal("JUAN PEREZ VARGAS", comprobante.RazonSocialCliente);
        Assert.Equal(new DateTime(2030, 6, 23, 9, 58, 0), comprobante.FechaVenta);
        Assert.Equal(20.50m, comprobante.Total);
        Assert.Equal("Son Veinte 50/100 Bolivianos", comprobante.TotalEnLetras);
        Assert.Equal(MomentoDeLaVenta, comprobante.FechaGeneracion);
        Assert.Equal("cajero1", comprobante.UsuarioGenerador);
        Assert.False(comprobante.EstaAnulada);
    }

    [Fact]
    public async Task El_pie_muestra_el_cajero_y_la_fecha_de_la_venta_sin_depender_de_quien_abre_el_comprobante()
    {
        var venta = Venta(detalle: [Fila(14, "Galletas Moka Arcor 110g", 1, 4.50m)]) with { UsuarioCajero = "maria" };

        var html = await HtmlDe(venta);

        Assert.Contains("23/06/2030 - 09:58:00-maria", html);
    }

    [Fact]
    public async Task Abrir_el_comprobante_varias_veces_devuelve_siempre_el_mismo_pie()
    {
        var venta = Venta(detalle: [Fila(14, "Galletas Moka Arcor 110g", 1, 4.50m)]);
        var servicio = Servicio(venta);
        var generador = new GeneradorComprobanteHtml();

        var primera = generador.GenerarHtml((await servicio.ObtenerAsync(7))!);
        var segunda = generador.GenerarHtml((await servicio.ObtenerAsync(7))!);

        Assert.Equal(primera, segunda);
        Assert.Contains("23/06/2030 - 09:58:00-cajero1", segunda);
    }

    [Fact]
    public async Task Dos_lotes_del_mismo_producto_se_agrupan_en_una_sola_linea()
    {
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

    private static async Task<string> HtmlDe(VentaParaComprobante venta)
    {
        var comprobante = await Servicio(venta).ObtenerAsync(7);
        Assert.NotNull(comprobante);

        return new GeneradorComprobanteHtml().GenerarHtml(comprobante);
    }

    [Fact]
    public async Task El_html_incluye_el_total_el_cajero_y_la_fecha_y_hora_de_la_venta()
    {
        var html = await HtmlDe(
            Venta(detalle:
            [
                Fila(14, "Galletas Moka Arcor 110g", 1, 4.50m),
                Fila(10, "Salsa de Tomate Kris 400g", 2, 8.00m),
                Fila(20, "Tostadas Trigo PIL 200g", 1, 7.50m)
            ]));

        Assert.Contains("COMPROBANTE DE VENTA", html);
        Assert.Contains("28,00", html);
        Assert.Contains("Son Veintiocho 00/100 Bolivianos", html);
        Assert.Contains("cajero1", html);
        Assert.Contains("23/06/2030", html);
        Assert.Contains("09:58:00", html);
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
            RazonSocialCliente = "<script>alert('x')</script>",
            UsuarioCajero = "<i>admin</i>"
        };

        var html = await HtmlDe(venta);

        Assert.DoesNotContain("<script>alert", html);
        Assert.DoesNotContain("<b>Leche</b>", html);
        Assert.DoesNotContain("<i>admin</i>", html);
        Assert.Contains("&lt;script&gt;", html);
    }

    [Fact]
    public async Task El_html_no_muestra_efectivo_ni_cambio()
    {
        var html = await HtmlDe(Venta(detalle: [Fila(1, "Leche", 1, 6.50m)]));

        Assert.DoesNotContain("Efectivo", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Cambio", html, StringComparison.OrdinalIgnoreCase);
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
