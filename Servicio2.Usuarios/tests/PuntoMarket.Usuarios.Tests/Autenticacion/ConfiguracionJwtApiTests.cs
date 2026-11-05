using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PuntoMarket.Usuarios.Api.Controllers;
using PuntoMarket.Usuarios.Api.DependencyInjection;
using PuntoMarket.Usuarios.Application.Ports.Autenticacion;
using Xunit;

namespace PuntoMarket.Usuarios.Tests.Autenticacion;

public class ConfiguracionJwtApiTests
{
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
        Assert.Equal(JwtTokenPruebaFactory.Emisor, validacion.ValidIssuer);
        Assert.True(validacion.ValidateAudience);
        Assert.Equal(JwtTokenPruebaFactory.Audiencia, validacion.ValidAudience);
        Assert.True(validacion.ValidateLifetime);
        Assert.True(validacion.RequireExpirationTime);
        Assert.Equal(TimeSpan.FromMinutes(1), validacion.ClockSkew);
        Assert.Equal("role", validacion.RoleClaimType);
        Assert.Equal("unique_name", validacion.NameClaimType);
        Assert.Equal([SecurityAlgorithms.HmacSha256], validacion.ValidAlgorithms);
        Assert.Equal(
            JwtTokenPruebaFactory.Clave,
            System.Text.Encoding.UTF8.GetString(
                Assert.IsType<SymmetricSecurityKey>(validacion.IssuerSigningKey).Key));
    }

    [Fact]
    public async Task Opciones_registradas_validan_tokens_reales_y_rechazan_firma_o_issuer_invalidos()
    {
        var servicios = new ServiceCollection();
        servicios.AddLogging();
        servicios.AddRouting();
        servicios.AgregarAutenticacionJwt(CrearConfiguracion());
        servicios.AddAuthorization();

        using var proveedor = servicios.BuildServiceProvider();
        var bearer = proveedor.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);
        var emisor = proveedor.GetRequiredService<IEmisorToken>();
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var claims = new[]
        {
            new Claim("unique_name", "ana"),
            new Claim("nombre", "Ana Pérez"),
            new Claim("role", "Administrador"),
            new Claim("debeCambiarPassword", "false")
        };
        var tokenValido = emisor.Emitir(42, "ana", "Ana Pérez", "Administrador", false);

        var principal = handler.ValidateToken(
            tokenValido,
            bearer.TokenValidationParameters,
            out _);

        Assert.Equal("42", principal.FindFirst("sub")?.Value);
        Assert.Equal("Administrador", principal.FindFirst("role")?.Value);
        Assert.Equal(StatusCodes.Status200OK, await EjecutarEndpointProtegidoAsync(proveedor, tokenValido));

        var tokenConOtraClave = JwtTokenPruebaFactory.CrearTokenFirmado(
            claims,
            key: JwtTokenPruebaFactory.ClaveAlternativa);
        Assert.ThrowsAny<SecurityTokenException>(() =>
            handler.ValidateToken(
                tokenConOtraClave,
                bearer.TokenValidationParameters,
                out _));

        var tokenConOtroIssuer = JwtTokenPruebaFactory.CrearTokenFirmado(
            claims,
            issuer: "Otro.Emisor");
        Assert.Throws<SecurityTokenInvalidIssuerException>(() =>
            handler.ValidateToken(
                tokenConOtroIssuer,
                bearer.TokenValidationParameters,
                out _));
    }

    private static async Task<int> EjecutarEndpointProtegidoAsync(
        IServiceProvider proveedor,
        string token)
    {
        var pipeline = new ApplicationBuilder(proveedor);
        pipeline.UseAuthentication();
        pipeline.UseAuthorization();
        pipeline.Run(context =>
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            return Task.CompletedTask;
        });
        var aplicacion = pipeline.Build();

        await using var scope = proveedor.CreateAsyncScope();
        var contexto = new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider
        };
        contexto.Request.Headers.Authorization = $"Bearer {token}";
        contexto.SetEndpoint(new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new AuthorizeAttribute { Roles = "Administrador" }),
            "Endpoint protegido"));

        await aplicacion(contexto);
        return contexto.Response.StatusCode;
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
                ["Jwt:Key"] = JwtTokenPruebaFactory.Clave,
                ["Jwt:ExpireMinutes"] = "60"
            })
            .Build();
}
