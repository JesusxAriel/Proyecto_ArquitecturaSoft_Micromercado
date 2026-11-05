using PuntoMarket.Reportes.Domain.Tendencias;
using PuntoMarket.Reportes.Domain.ValueObjects;
using PuntoMarket.Reportes.Domain.VentasPorCategoria;
using PuntoMarket.Reportes.Infrastructure.Exportacion.Comunes;
using PuntoMarket.Reportes.Infrastructure.Exportacion.VentasPorCategoria;

namespace PuntoMarket.Reportes.Tests.VentasPorCategoria;

public class GeneradorDeGraficoTests
{
    private static readonly byte[] FirmaPng = [0x89, 0x50, 0x4E, 0x47];

    private readonly GeneradorDeGrafico _generador = new();

    private static IReadOnlyList<FilaDeCategoria> Categorias(int cantidad) =>
        Enumerable.Range(1, cantidad)
            .Select(i => new FilaDeCategoria(i, $"Categoría con un nombre bastante largo número {i}", i, 1000m / i))
            .ToList();

    private static void AssertEsPng(ImagenDeGrafico imagen)
    {
        Assert.Equal(FirmaPng, imagen.Contenido.Take(4));
        Assert.True(imagen.Ancho > 0);
        Assert.True(imagen.Alto > 0);
        Assert.False(string.IsNullOrWhiteSpace(imagen.Titulo));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(15)]
    public void Las_barras_por_categoria_se_generan_con_cualquier_cantidad(int cantidad)
    {
        AssertEsPng(_generador.MontoPorCategoria(Categorias(cantidad)));
    }

    [Fact]
    public void Las_barras_por_categoria_agrupan_el_resto_en_otros_y_no_crecen_sin_limite()
    {
        var pocas = _generador.MontoPorCategoria(Categorias(GeneradorDeGrafico.MaximoDeCategoriasEnBarras));
        var muchas = _generador.MontoPorCategoria(Categorias(40));

        Assert.True(muchas.Alto <= pocas.Alto + 120);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(15)]
    public void La_dona_se_genera_con_cualquier_cantidad_de_categorias(int cantidad)
    {
        AssertEsPng(_generador.ParticipacionPorCategoria(Categorias(cantidad)));
    }

    [Fact]
    public void La_tendencia_se_genera_en_modo_diario_y_semanal()
    {
        var diaria = TendenciaDeVentas.Desde(
        [
            new PuntoDeTendencia(new DateOnly(2026, 5, 1), 10m),
            new PuntoDeTendencia(new DateOnly(2026, 5, 4), 30m)
        ]);
        var semanal = TendenciaDeVentas.Desde(
        [
            new PuntoDeTendencia(new DateOnly(2026, 1, 1), 10m),
            new PuntoDeTendencia(new DateOnly(2026, 4, 1), 30m)
        ]);

        AssertEsPng(_generador.TendenciaDelMonto(diaria));
        AssertEsPng(_generador.TendenciaDelMonto(semanal));
    }

    [Fact]
    public void Los_productos_mas_vendidos_se_grafican_con_nombres_largos()
    {
        var productos = Enumerable.Range(1, 5)
            .Select(i => new FilaDeProducto(i, $"Producto con un nombre extremadamente largo número {i}", i, 100m * i))
            .ToList();

        AssertEsPng(_generador.ProductosMasVendidos(productos));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(12)]
    [InlineData(60)]
    public void El_monto_por_dia_se_genera_con_pocos_y_muchos_dias(int dias)
    {
        var puntos = Enumerable.Range(0, dias)
            .Select(i => new PuntoDeTendencia(new DateOnly(2026, 1, 1).AddDays(i), 10m + i))
            .ToList();

        AssertEsPng(_generador.MontoPorDia(puntos));
    }

    [Fact]
    public void La_participacion_por_cliente_se_genera_con_un_solo_cliente()
    {
        AssertEsPng(_generador.ParticipacionPorCliente([new ParteDelTotal("Único cliente", 50m)]));
    }
}
