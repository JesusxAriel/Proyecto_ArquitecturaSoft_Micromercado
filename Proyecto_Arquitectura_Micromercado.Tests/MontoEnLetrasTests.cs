using Proyecto_Arquitectura_Micromercado.Application.Comprobantes;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class MontoEnLetrasTests
{
    // Se lee con cultura invariante para que la prueba no dependa de la configuracion regional.
    private static decimal Monto(string texto) =>
        decimal.Parse(texto, System.Globalization.CultureInfo.InvariantCulture);

    [Theory]
    [InlineData("0", "Son Cero 00/100 Bolivianos")]
    [InlineData("1", "Son Un 00/100 Bolivianos")]
    [InlineData("2", "Son Dos 00/100 Bolivianos")]
    [InlineData("10", "Son Diez 00/100 Bolivianos")]
    [InlineData("15", "Son Quince 00/100 Bolivianos")]
    [InlineData("16", "Son Dieciséis 00/100 Bolivianos")]
    [InlineData("20", "Son Veinte 00/100 Bolivianos")]
    [InlineData("21", "Son Veintiún 00/100 Bolivianos")]
    [InlineData("22", "Son Veintidós 00/100 Bolivianos")]
    [InlineData("23", "Son Veintitrés 00/100 Bolivianos")]
    [InlineData("26", "Son Veintiséis 00/100 Bolivianos")]
    [InlineData("30", "Son Treinta 00/100 Bolivianos")]
    [InlineData("31", "Son Treinta y Un 00/100 Bolivianos")]
    [InlineData("45", "Son Cuarenta y Cinco 00/100 Bolivianos")]
    [InlineData("99", "Son Noventa y Nueve 00/100 Bolivianos")]
    public void Escribe_los_numeros_menores_a_cien(string monto, string esperado)
    {
        Assert.Equal(esperado, MontoEnLetras.Convertir(Monto(monto)));
    }

    [Theory]
    [InlineData("100", "Son Cien 00/100 Bolivianos")]
    [InlineData("101", "Son Ciento Un 00/100 Bolivianos")]
    [InlineData("115", "Son Ciento Quince 00/100 Bolivianos")]
    [InlineData("200", "Son Doscientos 00/100 Bolivianos")]
    [InlineData("500", "Son Quinientos 00/100 Bolivianos")]
    [InlineData("999", "Son Novecientos Noventa y Nueve 00/100 Bolivianos")]
    public void Escribe_los_cientos(string monto, string esperado)
    {
        Assert.Equal(esperado, MontoEnLetras.Convertir(Monto(monto)));
    }

    [Theory]
    [InlineData("1000", "Son Mil 00/100 Bolivianos")]
    [InlineData("1001", "Son Mil Un 00/100 Bolivianos")]
    [InlineData("2000", "Son Dos Mil 00/100 Bolivianos")]
    [InlineData("21000", "Son Veintiún Mil 00/100 Bolivianos")]
    [InlineData("100000", "Son Cien Mil 00/100 Bolivianos")]
    [InlineData("123456", "Son Ciento Veintitrés Mil Cuatrocientos Cincuenta y Seis 00/100 Bolivianos")]
    public void Escribe_los_miles(string monto, string esperado)
    {
        Assert.Equal(esperado, MontoEnLetras.Convertir(Monto(monto)));
    }

    [Theory]
    [InlineData("1000000", "Son Un Millón 00/100 Bolivianos")]
    [InlineData("1000001", "Son Un Millón Un 00/100 Bolivianos")]
    [InlineData("2000000", "Son Dos Millones 00/100 Bolivianos")]
    [InlineData("12345678", "Son Doce Millones Trescientos Cuarenta y Cinco Mil Seiscientos Setenta y Ocho 00/100 Bolivianos")]
    [InlineData("99999999.99", "Son Noventa y Nueve Millones Novecientos Noventa y Nueve Mil Novecientos Noventa y Nueve 99/100 Bolivianos")]
    public void Escribe_los_millones(string monto, string esperado)
    {
        Assert.Equal(esperado, MontoEnLetras.Convertir(Monto(monto)));
    }

    [Theory]
    [InlineData("31.5", "Son Treinta y Un 50/100 Bolivianos")]
    [InlineData("0.05", "Son Cero 05/100 Bolivianos")]
    [InlineData("0.99", "Son Cero 99/100 Bolivianos")]
    [InlineData("1250.75", "Son Mil Doscientos Cincuenta 75/100 Bolivianos")]
    public void Escribe_los_centavos_en_dos_cifras_sobre_cien(string monto, string esperado)
    {
        Assert.Equal(esperado, MontoEnLetras.Convertir(Monto(monto)));
    }

    [Theory]
    [InlineData("10.994", "Son Diez 99/100 Bolivianos")]
    [InlineData("10.995", "Son Once 00/100 Bolivianos")]
    [InlineData("0.999", "Son Un 00/100 Bolivianos")]
    public void Redondea_a_dos_decimales(string monto, string esperado)
    {
        Assert.Equal(esperado, MontoEnLetras.Convertir(Monto(monto)));
    }

    [Theory]
    [InlineData("-0.01")]
    [InlineData("1000000000")]
    public void Rechaza_montos_fuera_de_rango(string monto)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MontoEnLetras.Convertir(Monto(monto)));
    }
}
