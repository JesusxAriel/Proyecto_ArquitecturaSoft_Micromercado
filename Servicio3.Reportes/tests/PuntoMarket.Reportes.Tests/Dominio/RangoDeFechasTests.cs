using PuntoMarket.Reportes.Domain.Exceptions;
using PuntoMarket.Reportes.Domain.ValueObjects;

namespace PuntoMarket.Reportes.Tests.Dominio;

public class RangoDeFechasTests
{
    [Fact]
    public void Crea_un_rango_valido()
    {
        var desde = new DateOnly(2026, 1, 1);
        var hasta = new DateOnly(2026, 1, 31);

        var rango = new RangoDeFechas(desde, hasta);

        Assert.Equal(desde, rango.Desde);
        Assert.Equal(hasta, rango.Hasta);
    }

    [Fact]
    public void Acepta_un_rango_de_un_solo_dia()
    {
        var dia = new DateOnly(2026, 1, 1);

        var rango = new RangoDeFechas(dia, dia);

        Assert.Equal(dia, rango.Desde);
        Assert.Equal(dia, rango.Hasta);
    }

    [Fact]
    public void Un_rango_invertido_lanza_excepcion()
    {
        var desde = new DateOnly(2026, 2, 1);
        var hasta = new DateOnly(2026, 1, 1);

        Assert.Throws<RangoDeFechasInvalidoException>(() => new RangoDeFechas(desde, hasta));
    }

    [Fact]
    public void Permite_limites_abiertos()
    {
        var soloDesde = new RangoDeFechas(new DateOnly(2026, 1, 1), null);
        var soloHasta = new RangoDeFechas(null, new DateOnly(2026, 1, 1));
        var sinLimites = RangoDeFechas.SinLimites();

        Assert.Null(soloDesde.Hasta);
        Assert.Null(soloHasta.Desde);
        Assert.Null(sinLimites.Desde);
        Assert.Null(sinLimites.Hasta);
    }
}
