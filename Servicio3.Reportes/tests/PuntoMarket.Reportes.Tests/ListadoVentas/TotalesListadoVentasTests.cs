using PuntoMarket.Reportes.Domain.ListadoVentas;

namespace PuntoMarket.Reportes.Tests.ListadoVentas;

public class TotalesListadoVentasTests
{
    private static FilaDeVenta Fila(int id, EstadoDeVenta estado, decimal monto) =>
        new(id, $"F-{id:0000}", new DateTime(2026, 1, 15, 10, 0, 0), "1234567", "Comercial Andina", "ventas01", estado, monto);

    [Fact]
    public void Cuenta_cada_estado_y_suma_solo_las_ventas_completadas()
    {
        var filas = new[]
        {
            Fila(1, EstadoDeVenta.Completada, 100m),
            Fila(2, EstadoDeVenta.Completada, 250.50m),
            Fila(3, EstadoDeVenta.Anulada, 40m)
        };

        var totales = TotalesListadoVentas.Calcular(filas);

        Assert.Equal(2, totales.CantidadDeCompletadas);
        Assert.Equal(1, totales.CantidadDeAnuladas);
        Assert.Equal(350.50m, totales.MontoTotalDeCompletadas);
        Assert.Equal(175.25m, totales.PromedioPorVenta);
    }

    [Fact]
    public void Sin_datos_los_totales_y_el_promedio_son_cero()
    {
        var totales = TotalesListadoVentas.Calcular([]);

        Assert.Equal(0, totales.CantidadDeCompletadas);
        Assert.Equal(0, totales.CantidadDeAnuladas);
        Assert.Equal(0m, totales.MontoTotalDeCompletadas);
        Assert.Equal(0m, totales.PromedioPorVenta);
    }

    [Fact]
    public void Con_solo_ventas_anuladas_el_promedio_es_cero()
    {
        var filas = new[] { Fila(1, EstadoDeVenta.Anulada, 90m) };

        var totales = TotalesListadoVentas.Calcular(filas);

        Assert.Equal(1, totales.CantidadDeAnuladas);
        Assert.Equal(0m, totales.MontoTotalDeCompletadas);
        Assert.Equal(0m, totales.PromedioPorVenta);
    }

    [Fact]
    public void El_promedio_se_redondea_a_dos_decimales()
    {
        var filas = new[]
        {
            Fila(1, EstadoDeVenta.Completada, 0.01m),
            Fila(2, EstadoDeVenta.Completada, 0.02m)
        };

        var totales = TotalesListadoVentas.Calcular(filas);

        Assert.Equal(0.02m, totales.PromedioPorVenta);
    }
}
