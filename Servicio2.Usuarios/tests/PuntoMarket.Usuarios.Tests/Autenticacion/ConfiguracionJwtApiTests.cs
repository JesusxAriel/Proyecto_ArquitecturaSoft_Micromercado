using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PuntoMarket.Usuarios.Api.Controllers;
using PuntoMarket.Usuarios.Api.DependencyInjection;
using PuntoMarket.Usuarios.Application.Ports.Autenticacion;
using PuntoMarket.Usuarios.Application.Ports.Comunes;
using PuntoMarket.Usuarios.Infrastructure.Security;
using Xunit;

namespace PuntoMarket.Usuarios.Tests.Autenticacion;

public class ConfiguracionJwtApiTests
{
    private const string Clave =
        "7f4e2a9c1b6d8e3f0a5c9d2e6b1f8a4c";
    private const string OtraClave =
        "6a3d8f1c5b9e2a7d4c0f6b3e8a1d5c9f";

    [Fact]
    public void Registra_bearer_y_configura_validacion_de_tokens_y_claims()
    {
        var configuracion = CrearConfiguracion();
        var servicios = new ServiceCollection();

        servicios.AgregarAutenticacionJwt(configuracion);

        using var proveedor = servicios.BuildServiceProvider();
        var autenticacion = proveedor.GetRequiredService<IOptions<AuthenticationOptions>>().Value;
        var bearer = proveedor.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);
        var validacion = bearer.TokenValidationParameters;

        Assert.Equal(JwtBearerDefaults.AuthenticationScheme, autenticacion.DefaultScheme);
        Assert.False(bearer.MapInboundClaims);
        Assert.True(validacion.ValidateIssuerSigningKey);
        Assert.True(validacion.ValidateIssuer);
        Assert.Equal("PuntoMarket.Usuarios", validacion.ValidIssuer);
        Assert.True(validacion.ValidateAudience);
        Assert.Equal("PuntoMarket", validacion.ValidAudience);
        Assert.True(validacion.ValidateLifetime);
        Assert.True(validacion.RequireExpirationTime);
        Assert.Equal(TimeSpan.FromMinutes(1), validacion.ClockSkew);
        Assert.Equal("role", validacion.RoleClaimType);
        Assert.Equal("unique_name", validacion.NameClaimType);
        Assert.Equal([SecurityAlgorithms.HmacSha256], validacion.ValidAlgorithms);
        Assert.Equal(
            Clave,
            System.Text.Encoding.UTF8.GetString(
                Assert.IsType<SymmetricSecurityKey>(validacion.IssuerSigningKey).Key));
    }

    [Fact]
    public void Opciones_registradas_validan_tokens_reales_y_rechazan_firma_o_issuer_invalidos()
    {
        var servicios = new ServiceCollection();
        servicios.AgregarAutenticacionJwt(CrearConfiguracion());

        using var proveedor = servicios.BuildServiceProvider();
        var bearer = proveedor.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);
        var emisor = proveedor.GetRequiredService<IEmisorToken>();
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var tokenValido = emisor.Emitir(42, "ana", "Ana Pérez", "Administrador", false);

        var principal = handler.ValidateToken(
            tokenValido,
            bearer.TokenValidationParameters,
            out _);

        Assert.Equal("42", principal.FindFirst("sub")?.Value);
        Assert.Equal("Administrador", principal.FindFirst("role")?.Value);

        var reloj = proveedor.GetRequiredService<IReloj>();
        var tokenConOtraClave = CrearEmisor(
            "PuntoMarket.Usuarios",
            OtraClave,
            reloj).Emitir(42, "ana", "Ana Pérez", "Administrador", false);
        Assert.ThrowsAny<SecurityTokenException>(() =>
            handler.ValidateToken(
                tokenConOtraClave,
                bearer.TokenValidationParameters,
                out _));

        var tokenConOtroIssuer = CrearEmisor(
            "Otro.Emisor",
            Clave,
            reloj).Emitir(42, "ana", "Ana Pérez", "Administrador", false);
        Assert.Throws<SecurityTokenInvalidIssuerException>(() =>
            handler.ValidateToken(
                tokenConOtroIssuer,
                bearer.TokenValidationParameters,
                out _));
    }

    [Fact]
    public void Rechaza_configuracion_jwt_invalida_al_registrar_el_api()
    {
        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "PuntoMarket.Usuarios",
                ["Jwt:Audience"] = "PuntoMarket",
                ["Jwt:Key"] = "short",
                ["Jwt:ExpireMinutes"] = "60"
            })
            .Build();

        Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AgregarAutenticacionJwt(configuracion));
    }

    [Fact]
    public void Activacion_sigue_publica_y_gestion_exige_rol_administrador()
    {
        var activacion = Assert.Single(typeof(ActivacionController)
            .GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true));
        var autorizacion = Assert.Single(typeof(UsuariosController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true));

        Assert.IsType<AllowAnonymousAttribute>(activacion);
        Assert.Equal("Administrador", Assert.IsType<AuthorizeAttribute>(autorizacion).Roles);
    }

    private static IConfiguration CrearConfiguracion() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "PuntoMarket.Usuarios",
                ["Jwt:Audience"] = "PuntoMarket",
                ["Jwt:Key"] = Clave,
                ["Jwt:ExpireMinutes"] = "60"
            })
            .Build();

    private static JwtEmisorToken CrearEmisor(string issuer, string key, IReloj reloj) =>
        new(new JwtEmisorTokenOpciones
        {
            Issuer = issuer,
            Audience = "PuntoMarket",
            Key = key,
            ExpireMinutes = 60
        }, reloj);
}
