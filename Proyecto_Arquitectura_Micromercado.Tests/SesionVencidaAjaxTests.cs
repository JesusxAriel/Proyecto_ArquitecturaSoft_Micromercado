using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Web;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class SesionVencidaAjaxTests
{
    private static (DefaultHttpContext Contexto, RedirectContext<CookieAuthenticationOptions> Redireccion) Crear(string? aceptar)
    {
        var contexto = new DefaultHttpContext();
        contexto.Response.Body = new MemoryStream();
        if (aceptar is not null) contexto.Request.Headers.Accept = aceptar;

        var esquema = new AuthenticationScheme("Cookies", null, typeof(CookieAuthenticationHandler));
        var redireccion = new RedirectContext<CookieAuthenticationOptions>(
            contexto, esquema, new CookieAuthenticationOptions(), new AuthenticationProperties(), "/Login?ReturnUrl=%2FSales");
        return (contexto, redireccion);
    }

    [Fact]
    public async Task Una_peticion_de_datos_con_sesion_vencida_recibe_401_con_json()
    {
        var (contexto, redireccion) = Crear("application/json");

        await SesionVencidaAjax.ResponderAsync(redireccion);

        Assert.Equal(401, contexto.Response.StatusCode);
        contexto.Response.Body.Position = 0;
        var json = await JsonDocument.ParseAsync(contexto.Response.Body);
        Assert.False(json.RootElement.GetProperty("exito").GetBoolean());
        Assert.True(json.RootElement.GetProperty("sesionVencida").GetBoolean());
        Assert.Equal("Su sesión venció. Inicie sesión nuevamente para continuar.", json.RootElement.GetProperty("mensaje").GetString());
    }

    [Theory]
    [InlineData("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8")]
    [InlineData(null)]
    public async Task Una_navegacion_normal_sigue_redirigiendo_al_login(string? aceptar)
    {
        var (contexto, redireccion) = Crear(aceptar);

        await SesionVencidaAjax.ResponderAsync(redireccion);

        Assert.Equal(302, contexto.Response.StatusCode);
        Assert.Equal("/Login?ReturnUrl=%2FSales", contexto.Response.Headers.Location.ToString());
    }
}
