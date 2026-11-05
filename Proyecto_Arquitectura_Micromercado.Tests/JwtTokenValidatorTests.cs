using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Proyecto_Arquitectura_Micromercado.Application.Security;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Security;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public sealed class JwtTokenValidatorTests
{
    private const string Issuer = "Issuer-De-Prueba";
    private const string Audience = "Audience-De-Prueba";
    private const string Clave =
        "ClaveFicticiaSoloParaPruebas_0123456789";
    private static readonly DateTime AhoraUtc = DateTime.UtcNow;

    [Fact]
    public void Valida_firma_emisor_audiencia_y_claims_sin_mapear_nombres()
    {
        var validador = CrearValidador();
        var principal = validador.Validar(CrearToken());

        Assert.Equal("42", Assert.Single(principal.FindAll("sub")).Value);
        Assert.Equal("ana", Assert.Single(principal.FindAll("unique_name")).Value);
        Assert.Equal("Ana Pérez", Assert.Single(principal.FindAll("nombre")).Value);
        Assert.Equal("Administrador", Assert.Single(principal.FindAll("role")).Value);
        Assert.Equal("false", Assert.Single(principal.FindAll("debeCambiarPassword")).Value);
        Assert.Empty(principal.FindAll(ClaimTypes.NameIdentifier));
        Assert.Empty(principal.FindAll(ClaimTypes.Role));
    }

    [Fact]
    public void Rechaza_firma_invalida()
    {
        var token = CrearToken(key: "OtraClaveFicticiaSoloParaPruebas_9876543210");

        Assert.ThrowsAny<SecurityTokenException>(() => CrearValidador().Validar(token));
    }

    [Fact]
    public void Rechaza_emisor_invalido()
    {
        var token = CrearToken(issuer: "Otro-Emisor");

        Assert.Throws<SecurityTokenInvalidIssuerException>(
            () => CrearValidador().Validar(token));
    }

    [Fact]
    public void Rechaza_audiencia_invalida()
    {
        var token = CrearToken(audience: "Otra-Audiencia");

        Assert.Throws<SecurityTokenInvalidAudienceException>(
            () => CrearValidador().Validar(token));
    }

    [Fact]
    public void Rechaza_token_expirado()
    {
        var token = CrearToken(
            notBefore: AhoraUtc.AddMinutes(-5),
            expires: AhoraUtc.AddMinutes(-3));

        Assert.Throws<SecurityTokenExpiredException>(
            () => CrearValidador().Validar(token));
    }

    [Fact]
    public void Rechaza_token_antes_de_not_before()
    {
        var token = CrearToken(
            notBefore: AhoraUtc.AddMinutes(5),
            expires: AhoraUtc.AddMinutes(10));

        Assert.Throws<SecurityTokenNotYetValidException>(
            () => CrearValidador().Validar(token));
    }

    [Fact]
    public void Rechaza_algoritmo_distinto_de_hs256()
    {
        var token = CrearToken(
            key: "OtraClaveFicticiaSoloParaPruebas_9876543210_abcdefghijk",
            algorithm: SecurityAlgorithms.HmacSha384);
        Assert.Equal(SecurityAlgorithms.HmacSha384, new JwtSecurityTokenHandler().ReadJwtToken(token).Header.Alg);

        Assert.ThrowsAny<SecurityTokenException>(
            () => CrearValidador().Validar(token));
    }

    [Theory]
    [InlineData("sub")]
    [InlineData("unique_name")]
    [InlineData("nombre")]
    [InlineData("role")]
    [InlineData("debeCambiarPassword")]
    public void Rechaza_claim_requerido_ausente(string claimAusente)
    {
        var token = CrearToken(omitirClaim: claimAusente);

        Assert.Throws<SecurityTokenValidationException>(
            () => CrearValidador().Validar(token));
    }

    [Theory]
    [InlineData("sub", "0")]
    [InlineData("sub", "no-es-id")]
    [InlineData("debeCambiarPassword", "quizas")]
    [InlineData("unique_name", " ")]
    [InlineData("nombre", " ")]
    [InlineData("role", " ")]
    public void Rechaza_valores_de_claim_invalidos(string tipo, string valor)
    {
        var token = CrearToken(sobrescribirClaim: (tipo, valor));

        Assert.Throws<SecurityTokenValidationException>(
            () => CrearValidador().Validar(token));
    }

    [Fact]
    public void Rechaza_configuracion_sin_emisor_o_audiencia()
    {
        var sinEmisor = new JwtValidacionOpciones
        {
            Audience = Audience,
            Key = Clave
        };
        var sinAudiencia = new JwtValidacionOpciones
        {
            Issuer = Issuer,
            Key = Clave
        };

        Assert.Contains("Jwt:Issuer", Assert.Throws<InvalidOperationException>(
            sinEmisor.Validar).Message);
        Assert.Contains("Jwt:Audience", Assert.Throws<InvalidOperationException>(
            sinAudiencia.Validar).Message);
    }

    [Theory]
    [InlineData("Issuer", " Issuer-De-Prueba")]
    [InlineData("Issuer", "Issuer-De-Prueba ")]
    [InlineData("Issuer", "Issuer\tDe-Prueba")]
    [InlineData("Audience", " Audience-De-Prueba")]
    [InlineData("Audience", "Audience-De-Prueba ")]
    [InlineData("Audience", "Audience\rDe-Prueba")]
    public void Rechaza_emisor_o_audiencia_con_espacios_externos_o_controles(
        string propiedad,
        string valor)
    {
        var opciones = CrearOpciones();
        if (propiedad == "Issuer")
            opciones.Issuer = valor;
        else
            opciones.Audience = valor;

        var exception = Assert.Throws<InvalidOperationException>(opciones.Validar);

        Assert.Contains($"Jwt:{propiedad}", exception.Message);
    }

    [Fact]
    public void Acepta_clave_de_32_bytes_con_caracteres_repetidos()
    {
        var opciones = CrearOpciones();
        opciones.Key = new string('a', 32);

        opciones.Validar();
    }

    [Fact]
    public void Rechaza_configuracion_ausente_o_clave_invalida()
    {
        var opciones = Options.Create(new JwtValidacionOpciones
        {
            Issuer = Issuer,
            Audience = Audience,
            Key = "corta"
        });

        var exception = Assert.Throws<InvalidOperationException>(
            () => new JwtTokenValidator(opciones));

        Assert.Contains("Jwt:Key", exception.Message);
        Assert.DoesNotContain("corta", exception.Message);
    }

    [Fact]
    public void Valida_configuracion_al_iniciar_y_no_al_primero_resolver_el_servicio()
    {
        var configuration = CrearConfiguracion();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddJwtTokenValidation(configuration);

        using var provider = services.BuildServiceProvider();

        Assert.IsAssignableFrom<IValidadorJwt>(
            provider.GetRequiredService<IValidadorJwt>());
        Assert.Equal(Issuer, provider.GetRequiredService<IOptions<JwtValidacionOpciones>>().Value.Issuer);
    }

    [Fact]
    public void La_validacion_al_inicio_rechaza_configuracion_incompleta_sin_revelar_la_clave()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = Issuer,
                ["Jwt:Audience"] = Audience
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddJwtTokenValidation(configuration);

        using var provider = services.BuildServiceProvider();
        var exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IStartupValidator>().Validate());

        Assert.Contains(exception.Failures, error => error.Contains("Jwt:Key"));
        Assert.DoesNotContain("Clave", string.Join(" ", exception.Failures));
    }

    private static JwtTokenValidator CrearValidador() =>
        new(Options.Create(CrearOpciones()));

    private static JwtValidacionOpciones CrearOpciones() =>
        new()
        {
            Issuer = Issuer,
            Audience = Audience,
            Key = Clave
        };

    private static string CrearToken(
        string issuer = Issuer,
        string audience = Audience,
        string key = Clave,
        string algorithm = SecurityAlgorithms.HmacSha256,
        DateTime? notBefore = null,
        DateTime? expires = null,
        string? omitirClaim = null,
        (string Tipo, string Valor)? sobrescribirClaim = null)
    {
        var claims = new Dictionary<string, string>
        {
            ["sub"] = "42",
            ["unique_name"] = "ana",
            ["nombre"] = "Ana Pérez",
            ["role"] = "Administrador",
            ["debeCambiarPassword"] = "false"
        };

        if (omitirClaim is not null)
            claims.Remove(omitirClaim);
        if (sobrescribirClaim is { } reemplazo)
            claims[reemplazo.Tipo] = reemplazo.Valor;

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var signingCredentials = new SigningCredentials(signingKey, algorithm);
        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims.Select(claim => new Claim(claim.Key, claim.Value)),
            notBefore ?? AhoraUtc.AddMinutes(-1),
            expires ?? AhoraUtc.AddMinutes(10),
            signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static IConfiguration CrearConfiguracion() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = Issuer,
                ["Jwt:Audience"] = Audience,
                ["Jwt:Key"] = Clave
            })
            .Build();
}
