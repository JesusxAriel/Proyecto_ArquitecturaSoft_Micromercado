using PuntoMarket.Reportes.Domain.ListadoVentas;
using PuntoMarket.Reportes.Domain.ValueObjects;
using PuntoMarket.Reportes.Domain.VentasPorCategoria;

namespace PuntoMarket.Reportes.Tests.Dominio;

public class ResumenesDelReporteTests
{
    private static FilaDeVenta Venta(int id, int dia, string nit, string cliente, EstadoDeVenta estado, decimal monto) =>
        new(id, $"F-{id:0000}", new DateTime(2026, 5, dia, 10, 0, 0), nit, cliente, "ventas01", estado, monto);

    private static ReporteListadoVentas Reporte(params FilaDeVenta[] filas) =>
        ReporteListadoVentas.Crear(
            filas,
            new FiltrosDeListadoVentas(RangoDeFechas.SinLimites(), null, null, null),
            new DateTime(2026, 5, 20, 9, 0, 0),
            "admin");

    [Fact]
    public void El_monto_por_dia_suma_solo_las_ventas_completadas_y_ordena_por_fecha()
    {
        var reporte = Reporte(
            Venta(1, 3, "1", "Ana", EstadoDeVenta.Completada, 100m),
            Venta(2, 1, "2", "Beto", EstadoDeVenta.Completada, 40m),
            Venta(3, 3, "1", "Ana", EstadoDeVenta.Completada, 60m),
            Venta(4, 2, "3", "Carla", EstadoDeVenta.Anulada, 999m));

        var puntos = reporte.MontoPorDia();

        Assert.Equal(2, puntos.Count);
        Assert.Equal(new DateOnly(2026, 5, 1), puntos[0].Fecha);
        Assert.Equal(160m, puntos[1].Monto);
    }

    [Fact]
    public void La_participacion_por_cliente_agrupa_por_cliente_y_resume_el_resto_en_otros()
    {
        var reporte = Reporte(
            Venta(1, 1, "1", "Ana", EstadoDeVenta.Completada, 100m),
            Venta(2, 2, "1", "Ana", EstadoDeVenta.Completada, 50m),
            Venta(3, 2, "2", "Beto", EstadoDeVenta.Completada, 80m),
            Venta(4, 3, "3", "Carla", EstadoDeVenta.Completada, 20m),
            Venta(5, 3, "4", "Dani", EstadoDeVenta.Completada, 10m),
            Venta(6, 4, "5", "Anulado", EstadoDeVenta.Anulada, 500m));

        var partes = reporte.ParticipacionPorCliente(2);

        Assert.Equal(["Ana", "Beto", "Otros (2)"], partes.Select(p => p.Nombre));
        Assert.Equal([150m, 80m, 30m], partes.Select(p => p.Monto));
    }

    [Fact]
    public void Los_totales_incluyen_la_venta_mas_alta_solo_entre_las_completadas()
    {
        var reporte = Reporte(
            Venta(1, 1, "1", "Ana", EstadoDeVenta.Completada, 100m),
            Venta(2, 2, "2", "Beto", EstadoDeVenta.Completada, 80m),
            Venta(3, 3, "3", "Carla", EstadoDeVenta.Anulada, 900m));

        Assert.Equal(100m, reporte.Totales.VentaMasAlta);
        Assert.Equal(90m, reporte.Totales.PromedioPorVenta);
    }

    [Fact]
    public void Sin_ventas_los_resumenes_estan_vacios()
    {
        var reporte = Reporte();

        Assert.Empty(reporte.MontoPorDia());
        Assert.Empty(reporte.ParticipacionPorCliente(5));
        Assert.Equal(0m, reporte.Totales.VentaMasAlta);
    }

    [Fact]
    public void La_participacion_de_cada_categoria_se_calcula_sobre_el_monto_total()
    {
        var filas = new[]
        {
            new FilaDeCategoria(1, "Bebidas", 10, 75m),
            new FilaDeCategoria(2, "Lácteos", 4, 25m)
        };

        var totales = TotalesPorCategoria.Calcular(filas);

        Assert.Equal(0.75m, totales.Participacion(filas[0]));
        Assert.Equal(0.25m, totales.Participacion(filas[1]));
        Assert.Equal(0.75m, totales.ParticipacionDelLider);
    }

    [Fact]
    public void Sin_categorias_la_participacion_es_cero()
    {
        var totales = TotalesPorCategoria.Calcular([]);

        Assert.Equal(0m, totales.ParticipacionDelLider);
        Assert.Equal(0m, totales.Participacion(new FilaDeCategoria(1, "X", 1, 10m)));
    }
}
