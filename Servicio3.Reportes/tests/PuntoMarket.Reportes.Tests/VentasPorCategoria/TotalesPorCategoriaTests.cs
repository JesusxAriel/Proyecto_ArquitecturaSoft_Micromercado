using PuntoMarket.Reportes.Domain.VentasPorCategoria;

namespace PuntoMarket.Reportes.Tests.VentasPorCategoria;

public class TotalesPorCategoriaTests
{
    [Fact]
    public void Suma_unidades_y_montos_y_elige_la_categoria_de_mayor_monto()
    {
        var filas = new[]
        {
            new FilaDeCategoria(1, "Bebidas", 10, 100m),
            new FilaDeCategoria(2, "Lácteos", 5, 250.50m),
            new FilaDeCategoria(3, "Limpieza", 2, 40m)
        };

        var totales = TotalesPorCategoria.Calcular(filas);

        Assert.Equal(17, totales.TotalUnidades);
        Assert.Equal(390.50m, totales.TotalMonto);
        Assert.Equal("Lácteos", totales.CategoriaLider?.NombreCategoria);
    }

    [Fact]
    public void Sin_datos_los_totales_son_cero_y_no_hay_categoria_lider()
    {
        var totales = TotalesPorCategoria.Calcular([]);

        Assert.Equal(0, totales.TotalUnidades);
        Assert.Equal(0m, totales.TotalMonto);
        Assert.Null(totales.CategoriaLider);
    }

    [Fact]
    public void En_caso_de_empate_gana_la_primera_categoria_de_la_lista()
    {
        var filas = new[]
        {
            new FilaDeCategoria(1, "Bebidas", 3, 90m),
            new FilaDeCategoria(2, "Lácteos", 9, 90m)
        };

        var totales = TotalesPorCategoria.Calcular(filas);

        Assert.Equal("Bebidas", totales.CategoriaLider?.NombreCategoria);
    }
}
