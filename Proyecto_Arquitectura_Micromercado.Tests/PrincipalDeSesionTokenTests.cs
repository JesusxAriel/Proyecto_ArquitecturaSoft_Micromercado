using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Proyecto_Arquitectura_Micromercado.Pages.Shared;
using Proyecto_Arquitectura_Micromercado.Servicios.Reportes;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class PrincipalDeSesionTokenTests
{
    private const string Token = "token-emitido-por-usuarios";

    private static TokenDeAccesoDelUsuario CrearServicio(System.Security.Claims.ClaimsPrincipal usuario, string entorno)
    {
        var contexto = new DefaultHttpContext { User = usuario };
        return new TokenDeAccesoDelUsuario(
            new HttpContextAccessor { HttpContext = contexto },
            new ConfigurationBuilder().Build(),
            new EntornoFalso(entorno));
    }

    [Fact]
    public void El_principal_creado_en_el_login_incluye_el_token_de_acceso()
    {
        var principal = PrincipalDeSesion.Crear(
            1, "admin", "Administrador", "Cookies", false, null, Token);

        Assert.Equal(
            Token,
            principal.FindFirst(TokenDeAccesoDelUsuario.ClaimDeTokenDeAcceso)?.Value);
    }

    [Fact]
    public void Sin_token_el_principal_no_incluye_la_claim()
    {
        var principal = PrincipalDeSesion.Crear(1, "admin", "Administrador", "Cookies");

        Assert.Null(principal.FindFirst(TokenDeAccesoDelUsuario.ClaimDeTokenDeAcceso));
    }

    [Fact]
    public void El_servicio_devuelve_el_token_del_principal_de_sesion()
    {
        var principal = PrincipalDeSesion.Crear(
            1, "admin", "Administrador", "Cookies", false, null, Token);

        var token = CrearServicio(principal, Environments.Production).Obtener();

        Assert.Equal(Token, token);
    }

    [Fact]
    public void En_produccion_un_principal_sin_token_lanza_la_excepcion()
    {
        var principal = PrincipalDeSesion.Crear(1, "admin", "Administrador", "Cookies");

        var servicio = CrearServicio(principal, Environments.Production);

        Assert.Throws<TokenDeAccesoNoDisponibleException>(servicio.Obtener);
    }

    private sealed class EntornoFalso(string nombre) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = nombre;

        public string ApplicationName { get; set; } = "Pruebas";

        public string ContentRootPath { get; set; } = string.Empty;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
