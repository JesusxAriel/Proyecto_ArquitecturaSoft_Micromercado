using PuntoMarket.Ventas.Frontend.Clients;
using PuntoMarket.Ventas.Frontend.Pages.Reportes;
namespace PuntoMarket.Ventas.Tests.Frontend;

public sealed class IndicadoresReporteTests
{
    private static readonly DateTime Generado = new(2026, 11, 5, 9, 30, 0);

    [Fact]
    public void El_listado_resume_completadas_monto_promedio_venta_mas_alta_y_anuladas()
    {
        var reporte = new ListadoVentasReporte(
            [],
            new TotalesVentasReporte(4, 2, 400m, 100m, 180m),
            new FiltrosVentasReporte(null, null, null, null, 1000),
            Generado,
            "admin");

        var indicadores = IndicadorReporte.DeListado(reporte);

        Assert.Equal(
            ["Ventas completadas", "Monto total", "Promedio por venta", "Venta más alta", "Ventas anuladas"],
            indicadores.Select(i => i.Etiqueta));
        Assert.Equal("4", indicadores[0].Valor);
        Assert.Equal("Bs 400,00", indicadores[1].Valor);
        Assert.Equal("Bs 100,00", indicadores[2].Valor);
        Assert.Equal("Bs 180,00", indicadores[3].Valor);
        Assert.Equal("2", indicadores[4].Valor);
    }

    [Fact]
    public void Las_categorias_resumen_monto_unidades_cantidad_y_lider_con_su_participacion()
    {
        var reporte = new CategoriasReporte(
            [new FilaCategoriaReporte(1, "Lácteos", 5, 75m), new FilaCategoriaReporte(2, "Bebidas", 8, 25m)],
            new TotalesCategoriasReporte(13, 100m, "Lácteos"),
            new FiltrosCategoriasReporte(null, null, null, null),
            Generado,
            "admin");

        var indicadores = IndicadorReporte.DeCategorias(reporte);

        Assert.Equal(["Monto total", "Unidades vendidas", "Categorías", "Categoría líder"], indicadores.Select(i => i.Etiqueta));
        Assert.Equal("Bs 100,00", indicadores[0].Valor);
        Assert.Equal("13", indicadores[1].Valor);
        Assert.Equal("2", indicadores[2].Valor);
        Assert.Equal("Lácteos", indicadores[3].Valor);
        Assert.Contains("Bs 75,00", indicadores[3].Detalle);
        Assert.Contains("75", indicadores[3].Detalle);
        Assert.Contains("%", indicadores[3].Detalle);
    }

    [Fact]
    public void Sin_categorias_el_lider_no_tiene_detalle_ni_divide_por_cero()
    {
        var reporte = new CategoriasReporte(
            [],
            new TotalesCategoriasReporte(0, 0m, null),
            new FiltrosCategoriasReporte(null, null, null, null),
            Generado,
            "admin");

        var lider = IndicadorReporte.DeCategorias(reporte)[3];

        Assert.Equal("Sin datos", lider.Valor);
        Assert.Null(lider.Detalle);
    }

    [Fact]
    public void El_contrato_del_listado_acepta_respuestas_sin_la_venta_mas_alta()
    {
        const string json = """{"filas":[],"totales":{"ventasCompletadas":1,"ventasAnuladas":0,"montoTotalDeCompletadas":10,"promedioPorVenta":10},"filtros":{"tope":1000},"generadoEn":"2026-11-03T09:15:00","generadoPor":"admin"}""";

        var respuesta = System.Text.Json.JsonSerializer.Deserialize<ListadoVentasReporte>(
            json,
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));

        Assert.Equal(0m, respuesta!.Totales.VentaMasAlta);
    }
}
