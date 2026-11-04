using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using PuntoMarket.Usuarios.Application.Ports.Comunes;
using PuntoMarket.Usuarios.Infrastructure.Security;
using Xunit;

namespace PuntoMarket.Usuarios.Tests.Autenticacion;

public class JwtEmisorTokenTests
{
    private const string Clave =
        "7f4e2a9c1b6d8e3f0a5c9d2e6b1f8a4c";

    private static readonly DateTimeOffset Ahora =
        new(2026, 11, 3, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Emite_token_con_claims_emisor_audiencia_y_firma_hs256_validos()
    {
        var emisor = CrearEmisor();

        var valor = emisor.Emitir(42, "ana", "Ana Pérez", "Administrador", true);
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var principal = handler.ValidateToken(
            valor,
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "PuntoMarket.Usuarios",
                ValidateAudience = true,
                ValidAudience = "PuntoMarket",
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Clave)),
                ValidateLifetime = false,
                RoleClaimType = "role",
                NameClaimType = "unique_name"
            },
            out var tokenValidado);

        Assert.Equal(
            SecurityAlgorithms.HmacSha256,
            Assert.IsType<JwtSecurityToken>(tokenValidado).Header.Alg);
        Assert.Equal("42", principal.FindFirst("sub")?.Value);
        Assert.Equal("ana", principal.FindFirst("unique_name")?.Value);
        Assert.Equal("Ana Pérez", principal.FindFirst("nombre")?.Value);
        Assert.Equal("Administrador", principal.FindFirst("role")?.Value);
        Assert.Equal("true", principal.FindFirst("debeCambiarPassword")?.Value);
        Assert.True(principal.IsInRole("Administrador"));
    }

    [Fact]
    public void Configura_la_expiracion_a_partir_del_reloj_y_la_duracion()
    {
        var emisor = CrearEmisor(expireMinutes: 45);

        var valor = emisor.Emitir(42, "ana", "Ana Pérez", "Administrador", false);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(valor);

        Assert.Equal(Ahora.UtcDateTime, token.ValidFrom);
        Assert.Equal(Ahora.AddMinutes(45).UtcDateTime, token.ValidTo);
        Assert.Equal("false", token.Claims.Single(claim =>
            claim.Type == "debeCambiarPassword").Value);
    }

    [Fact]
    public void Rechaza_token_firmado_con_otra_clave()
    {
        var valor = CrearEmisor().Emitir(42, "ana", "Ana Pérez", "Administrador", false);
        var handler = new JwtSecurityTokenHandler();

        Assert.ThrowsAny<SecurityTokenException>(() =>
            handler.ValidateToken(
                valor,
                new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes("Otra-clave-secreta-prueba-2026-abcdef")),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = false
                },
                out _));
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [InlineData("change_me_this_is_a_long_example_key_123")]
    public void Rechaza_secreto_vacio_corto_o_claramente_inseguro(string key)
    {
        var opciones = CrearOpciones();
        opciones.Key = key;

        Assert.Throws<InvalidOperationException>(() =>
            new JwtEmisorToken(opciones, new RelojFijo()));
    }

    [Fact]
    public void Rechaza_duracion_no_positiva()
    {
        var opciones = CrearOpciones();
        opciones.ExpireMinutes = 0;

        Assert.Throws<InvalidOperationException>(() =>
            new JwtEmisorToken(opciones, new RelojFijo()));
    }

    private static JwtEmisorToken CrearEmisor(int expireMinutes = 60) =>
        new(CrearOpciones(expireMinutes), new RelojFijo());

    private static JwtEmisorTokenOpciones CrearOpciones(int expireMinutes = 60) =>
        new()
        {
            Issuer = "PuntoMarket.Usuarios",
            Audience = "PuntoMarket",
            Key = Clave,
            ExpireMinutes = expireMinutes
        };

    private sealed class RelojFijo : IReloj
    {
        public DateTimeOffset UtcNow => Ahora;
    }
}
