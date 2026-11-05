using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using PuntoMarket.Usuarios.Infrastructure.Security;
using Xunit;

namespace PuntoMarket.Usuarios.Tests.Autenticacion;

public class JwtEmisorTokenTests
{
    [Fact]
    public void Emite_token_con_claims_emisor_audiencia_y_firma_hs256_validos()
    {
        var emisor = JwtTokenPruebaFactory.CrearEmisor();

        var valor = emisor.Emitir(42, "ana", "Ana Pérez", "Administrador", true);
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var principal = handler.ValidateToken(
            valor,
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = JwtTokenPruebaFactory.Emisor,
                ValidateAudience = true,
                ValidAudience = JwtTokenPruebaFactory.Audiencia,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(JwtTokenPruebaFactory.Clave)),
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
        var emisor = JwtTokenPruebaFactory.CrearEmisor(expireMinutes: 45);

        var valor = emisor.Emitir(42, "ana", "Ana Pérez", "Administrador", false);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(valor);

        Assert.Equal(JwtTokenPruebaFactory.Ahora.UtcDateTime, token.ValidFrom);
        Assert.Equal(JwtTokenPruebaFactory.Ahora.AddMinutes(45).UtcDateTime, token.ValidTo);
        Assert.Equal("false", token.Claims.Single(claim =>
            claim.Type == "debeCambiarPassword").Value);
    }

    [Fact]
    public void Rechaza_token_firmado_con_otra_clave()
    {
        var valor = JwtTokenPruebaFactory.CrearEmisor()
            .Emitir(42, "ana", "Ana Pérez", "Administrador", false);
        var handler = new JwtSecurityTokenHandler();

        Assert.ThrowsAny<SecurityTokenException>(() =>
            handler.ValidateToken(
                valor,
                new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(JwtTokenPruebaFactory.ClaveAlternativa)),
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
        var opciones = JwtTokenPruebaFactory.CrearOpciones();
        opciones.Key = key;

        Assert.Throws<InvalidOperationException>(() =>
            new JwtEmisorToken(opciones, JwtTokenPruebaFactory.CrearReloj()));
    }

    [Fact]
    public void Rechaza_duracion_no_positiva()
    {
        var opciones = JwtTokenPruebaFactory.CrearOpciones();
        opciones.ExpireMinutes = 0;

        Assert.Throws<InvalidOperationException>(() =>
            JwtTokenPruebaFactory.CrearEmisor(expireMinutes: 0));
    }
}
