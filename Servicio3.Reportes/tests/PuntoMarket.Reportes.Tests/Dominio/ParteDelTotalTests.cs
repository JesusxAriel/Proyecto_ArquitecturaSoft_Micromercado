using PuntoMarket.Reportes.Domain.ValueObjects;

namespace PuntoMarket.Reportes.Tests.Dominio;

public class ParteDelTotalTests
{
    private static IReadOnlyList<ParteDelTotal> Partes(params decimal[] montos) =>
        montos.Select((monto, indice) => new ParteDelTotal($"Parte {indice + 1}", monto)).ToList();

    [Fact]
    public void Si_no_supera_el_maximo_devuelve_las_partes_sin_cambios()
    {
        var partes = Partes(50m, 30m, 20m);

        var resultado = ParteDelTotal.TopConOtros(partes, 5);

        Assert.Equal(partes, resultado);
    }

    [Fact]
    public void Si_supera_el_maximo_agrupa_el_resto_en_otros_sin_perder_monto()
    {
        var partes = Partes(50m, 30m, 10m, 6m, 4m);

        var resultado = ParteDelTotal.TopConOtros(partes, 3);

        Assert.Equal(4, resultado.Count);
        Assert.Equal("Otros (2)", resultado[^1].Nombre);
        Assert.Equal(10m, resultado[^1].Monto);
        Assert.Equal(100m, resultado.Sum(p => p.Monto));
    }
}
