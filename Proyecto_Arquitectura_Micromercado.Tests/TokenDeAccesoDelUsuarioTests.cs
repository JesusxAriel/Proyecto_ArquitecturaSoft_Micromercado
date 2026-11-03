using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Proyecto_Arquitectura_Micromercado.Servicios.Reportes;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class TokenDeAccesoDelUsuarioTests
{
    private static TokenDeAccesoDelUsuario Crear(string entorno, string? claim = null, string? tokenDePrueba = null)
    {
        var contexto = new DefaultHttpContext();
        if (claim is not null)
            contexto.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(TokenDeAccesoDelUsuario.ClaimDeTokenDeAcceso, claim)], "Prueba"));

        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(tokenDePrueba is null
                ? []
                : new Dictionary<string, string?> { [TokenDeAccesoDelUsuario.ClaveDeTokenDePrueba] = tokenDePrueba })
            .Build();

        return new TokenDeAccesoDelUsuario(
            new HttpContextAccessor { HttpContext = contexto },
            configuracion,
            new EntornoFalso(entorno));
    }

    [Fact]
    public void El_claim_del_usuario_tiene_prioridad_sobre_el_token_de_prueba()
    {
        var token = Crear(Environments.Development, "token-del-claim", "token-de-prueba").Obtener();

        Assert.Equal("token-del-claim", token);
    }

    [Fact]
    public void Sin_claim_en_desarrollo_usa_el_token_de_prueba_de_la_configuracion()
    {
        var token = Crear(Environments.Development, tokenDePrueba: "token-de-prueba").Obtener();

        Assert.Equal("token-de-prueba", token);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void Fuera_de_desarrollo_ignora_el_token_de_prueba_y_lanza_excepcion(string entorno)
    {
        var servicio = Crear(entorno, tokenDePrueba: "token-de-prueba");

        Assert.Throws<TokenDeAccesoNoDisponibleException>(servicio.Obtener);
    }

    [Fact]
    public void Sin_claim_ni_token_de_prueba_lanza_excepcion_clara()
    {
        var servicio = Crear(Environments.Development);

        var error = Assert.Throws<TokenDeAccesoNoDisponibleException>(servicio.Obtener);

        Assert.Contains("token de acceso", error.Message);
    }

    [Fact]
    public void Un_claim_en_blanco_se_ignora()
    {
        var token = Crear(Environments.Development, "   ", "token-de-prueba").Obtener();

        Assert.Equal("token-de-prueba", token);
    }

    private sealed class EntornoFalso(string nombre) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = nombre;

        public string ApplicationName { get; set; } = "Pruebas";

        public string ContentRootPath { get; set; } = string.Empty;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
