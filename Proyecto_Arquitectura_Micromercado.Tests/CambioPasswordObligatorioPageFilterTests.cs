using System.Security.Claims;
using Proyecto_Arquitectura_Micromercado.Application.Security;
using Proyecto_Arquitectura_Micromercado.Pages.Shared;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class CambioPasswordObligatorioPageFilterTests
{
    [Theory]
    [InlineData("/Index", true)]
    [InlineData("/Users/Index", true)]
    [InlineData("/Users/CambiarPassword", false)]
    [InlineData("/Logout", false)]
    public void Usuario_obligado_solo_puede_ir_a_cambio_de_password_o_cerrar_sesion(
        string pagina,
        bool debeRedirigir)
    {
        var usuario = PrincipalDeSesion.Crear(1, "usuario", "Cajero", "Cookies", true);

        Assert.Equal(debeRedirigir, CambioPasswordObligatorioPageFilter.DebeRedirigir(usuario, pagina));
    }

    [Fact]
    public void Usuario_sin_cambio_obligatorio_no_es_redirigido()
    {
        var usuario = PrincipalDeSesion.Crear(1, "usuario", "Cajero", "Cookies");

        Assert.False(CambioPasswordObligatorioPageFilter.DebeRedirigir(usuario, "/Index"));
    }

    [Fact]
    public void Principal_no_autenticado_no_es_redirigido()
    {
        var usuario = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimsDeIdentidad.DebeCambiarPassword, bool.TrueString)]));

        Assert.False(CambioPasswordObligatorioPageFilter.DebeRedirigir(usuario, "/Index"));
    }
}
