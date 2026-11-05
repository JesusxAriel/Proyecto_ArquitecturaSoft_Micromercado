using PuntoMarket.Ventas.Tests.Integration;

namespace PuntoMarket.Ventas.Tests.Infrastructure;

public class ProteccionEntornoMySqlTests
{
    [Theory]
    [InlineData("bdMicroMercadoArqui")]
    [InlineData("mysql")]
    [InlineData("")]
    [InlineData("puntomarket_ventas_test_")]
    [InlineData("puntomarket_ventas_test_datos;DROP DATABASE mysql")]
    public void El_entorno_rechaza_nombres_no_exclusivos_de_pruebas(string nombre)
    {
        Assert.Throws<InvalidOperationException>(() => MySqlTestEnvironment.ValidarNombre(nombre));
    }

    [Fact]
    public void El_nombre_de_pruebas_aislado_es_admitido()
    {
        MySqlTestEnvironment.ValidarNombre("puntomarket_ventas_test_local_01");
    }
}
