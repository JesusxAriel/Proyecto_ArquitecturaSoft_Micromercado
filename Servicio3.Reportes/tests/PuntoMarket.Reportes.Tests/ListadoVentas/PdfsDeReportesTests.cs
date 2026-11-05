using System.Text;
using PuntoMarket.Reportes.Domain.ListadoVentas;
using PuntoMarket.Reportes.Domain.Tendencias;
using PuntoMarket.Reportes.Domain.ValueObjects;
using PuntoMarket.Reportes.Domain.VentasPorCategoria;
using PuntoMarket.Reportes.Infrastructure.Exportacion.ListadoVentas;
using PuntoMarket.Reportes.Infrastructure.Exportacion.VentasPorCategoria;

namespace PuntoMarket.Reportes.Tests.ListadoVentas;

public class PdfsDeReportesTests
{
    private static readonly GeneradorDeGrafico Grafico = new();

    private static ReporteListadoVentas Listado(int ventas)
    {
        var filas = Enumerable.Range(1, ventas)
            .Select(i => new FilaDeVenta(
                i,
                $"F-{i:0000}",
                new DateTime(2026, 5, 1).AddDays(i % 70).AddHours(8),
                (1000 + i % 9).ToString(),
                $"Cliente con una razón social bastante larga número {i % 9}",
                i % 2 == 0 ? "ventas01" : "ventas02",
                i % 8 == 0 ? EstadoDeVenta.Anulada : EstadoDeVenta.Completada,
                10m + i))
            .ToList();
        var filtros = new FiltrosDeListadoVentas(RangoDeFechas.SinLimites(), null, null, null);

        return ReporteListadoVentas.Crear(filas, filtros, new DateTime(2026, 11, 5, 9, 30, 15), "admin");
    }

    private static ReporteVentasPorCategoria PorCategoria(int categorias, bool conDetalle)
    {
        var filas = Enumerable.Range(1, categorias)
            .Select(i => new FilaDeCategoria(i, $"Categoría con un nombre muy largo número {i}", i * 2, 5000m / i))
            .ToList();
        var filtros = new FiltrosDeVentasPorCategoria(RangoDeFechas.SinLimites(), null, null);
        var reporte = ReporteVentasPorCategoria.Crear(filas, filtros, new DateTime(2026, 11, 5, 9, 30, 15), "admin");
        if (!conDetalle || categorias == 0)
            return reporte;

        var tendencia = TendenciaDeVentas.Desde(
            Enumerable.Range(0, 90).Select(i => new PuntoDeTendencia(new DateOnly(2026, 6, 1).AddDays(i), 50m + i)).ToList());
        var productos = Enumerable.Range(1, 5)
            .Select(i => new FilaDeProducto(i, $"Producto número {i} con nombre extenso", i, 100m * i))
            .ToList();

        return reporte.ConDetalle(tendencia, productos);
    }

    private static void AssertEsPdf(byte[] contenido)
    {
        Assert.StartsWith("%PDF", Encoding.ASCII.GetString(contenido, 0, 4));
        Assert.True(contenido.Length > 5_000);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(150)]
    [InlineData(400)]
    public void El_pdf_del_listado_se_genera_sin_datos_con_una_venta_y_con_muchas(int ventas)
    {
        var archivo = new ListadoVentasPdf(Grafico).Exportar(Listado(ventas));

        AssertEsPdf(archivo.Contenido);
    }

    [Fact]
    public void El_pdf_del_listado_con_solo_ventas_anuladas_no_grafica_y_se_genera()
    {
        var anuladas = Listado(16).Filas.Where(f => f.Estado == EstadoDeVenta.Anulada).ToList();
        var filtros = new FiltrosDeListadoVentas(RangoDeFechas.SinLimites(), EstadoDeVenta.Anulada, null, null);
        var reporte = ReporteListadoVentas.Crear(anuladas, filtros, new DateTime(2026, 11, 5, 9, 30, 15), "admin");

        AssertEsPdf(new ListadoVentasPdf(Grafico).Exportar(reporte).Contenido);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(1, false)]
    [InlineData(4, true)]
    [InlineData(20, true)]
    [InlineData(60, true)]
    public void El_pdf_de_ventas_por_categoria_se_genera_con_cualquier_cantidad_y_con_o_sin_detalle(int categorias, bool conDetalle)
    {
        var archivo = new VentasPorCategoriaPdf(Grafico).Exportar(PorCategoria(categorias, conDetalle));

        AssertEsPdf(archivo.Contenido);
    }

    [Fact]
    public void El_pdf_de_ventas_por_categoria_con_detalle_pesa_mas_que_sin_detalle()
    {
        var exportador = new VentasPorCategoriaPdf(Grafico);

        var sin = exportador.Exportar(PorCategoria(4, conDetalle: false));
        var con = exportador.Exportar(PorCategoria(4, conDetalle: true));

        Assert.True(con.Contenido.Length > sin.Contenido.Length);
    }
}
