using Proyecto_Arquitectura_Micromercado.Domain.Products;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class CambioDePrecioTests
{
    [Theory]
    [InlineData(10.0, 8.0, 10.0, 8.0, false)]
    [InlineData(10.0, 8.0, 12.0, 8.0, true)]
    [InlineData(10.0, 8.0, 10.0, 9.0, true)]
    [InlineData(10.0, 8.0, 12.0, 9.0, true)]
    public void Detecta_el_cambio_de_cualquiera_de_los_dos_precios(
        decimal ventaAnterior,
        decimal costoAnterior,
        decimal ventaNueva,
        decimal costoNuevo,
        bool esperado)
    {
        Assert.Equal(
            esperado,
            CambioDePrecio.Hubo(ventaAnterior, costoAnterior, ventaNueva, costoNuevo));
    }

    [Theory]
    [InlineData("ajuste de proveedor", "Ajuste de proveedor")]
    [InlineData("  ajuste   con    espacios  ", "Ajuste con espacios")]
    [InlineData("ajuste\tcon\ttabulaciones", "Ajuste con tabulaciones")]
    [InlineData("(ajuste) entre parentesis", "(Ajuste) entre parentesis")]
    [InlineData("2025: subida anual", "2025: Subida anual")]
    [InlineData("YA EN MAYUSCULAS", "YA EN MAYUSCULAS")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void Normaliza_espacios_y_la_primera_letra(string? entrada, string esperado)
    {
        Assert.Equal(esperado, CambioDePrecio.NormalizarMotivo(entrada));
    }

    [Theory]
    [InlineData("Ajuste de proveedor")]
    [InlineData("abc")]
    public void Acepta_motivos_validos(string motivo)
    {
        Assert.True(CambioDePrecio.EsMotivoValido(motivo));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ab")]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("comillas \" dobles")]
    [InlineData("comilla ' simple")]
    [InlineData("acento ` grave")]
    [InlineData("barra \\ invertida")]
    [InlineData("punto ; y coma")]
    [InlineData("control \u0001 incrustado")]
    public void Rechaza_motivos_invalidos(string? motivo)
    {
        Assert.False(CambioDePrecio.EsMotivoValido(motivo));
    }

    [Fact]
    public void Sin_cambio_de_precio_el_motivo_no_corresponde()
    {
        Assert.Null(CambioDePrecio.ResolverMotivo(huboCambio: false, "lo que sea"));
    }

    [Fact]
    public void Con_cambio_de_precio_el_motivo_se_normaliza()
    {
        Assert.Equal(
            "Subida del proveedor",
            CambioDePrecio.ResolverMotivo(huboCambio: true, "  subida   del proveedor "));
    }
}
