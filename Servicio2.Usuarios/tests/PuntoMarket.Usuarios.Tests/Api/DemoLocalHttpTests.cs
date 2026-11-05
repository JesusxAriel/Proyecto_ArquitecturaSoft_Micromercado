using System.Net;
using System.Net.Http.Headers;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using PuntoMarket.Usuarios.Api;
using PuntoMarket.Usuarios.Infrastructure.Security;
using PuntoMarket.Usuarios.Infrastructure.Time;
using Swashbuckle.AspNetCore.Swagger;
using Xunit;
namespace PuntoMarket.Usuarios.Tests.Api;
public sealed class DemoLocalHttpTests
{
    [Theory]
    [InlineData("Development",true)] [InlineData("LocalDemo",true)]
    [InlineData("Production",false)] [InlineData("Staging",false)] [InlineData("Testing",false)]
    public async Task Swagger_disponible_solo_en_entornos_locales_y_negocio_protegido(string entorno, bool disponible)
    {
        var clave = Convert.ToHexString(RandomNumberGenerator.GetBytes(48));
        await using var app = UsuariosBootstrap.Crear(["--environment",entorno], b => {
            b.Logging.ClearProviders();
            b.Configuration.AddInMemoryCollection(new Dictionary<string,string?> {
                ["Jwt:Key"]=clave,["Jwt:Issuer"]="PuntoMarket.DemoLocal",["Jwt:Audience"]="PuntoMarket.Ventas.DemoLocal"
            });
        });
        if (entorno == "LocalDemo") Assert.Equal("http://127.0.0.1:5003",app.Configuration["urls"]);
        // Puerto efímero para pruebas; no se abre MySQL ni se ejecuta el lanzador real.
        app.Urls.Clear(); app.Urls.Add("http://127.0.0.1:0");
        await app.StartAsync();
        using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect=false }) { BaseAddress = new(app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single()) };
        Assert.Equal(disponible, app.Services.GetService<ISwaggerProvider>() is not null);
        foreach (var ruta in new[] {"/swagger/index.html","/swagger/v1/swagger.json","/swagger/swagger-ui-bundle.js"})
            Assert.Equal(disponible ? HttpStatusCode.OK : HttpStatusCode.NotFound,(await http.GetAsync(ruta)).StatusCode);
        if (disponible)
        {
            using var doc = JsonDocument.Parse(await http.GetStringAsync("/swagger/v1/swagger.json"));
            var paths=doc.RootElement.GetProperty("paths");
            Assert.True(paths.TryGetProperty("/api/autenticacion/login",out _));
            Assert.True(paths.TryGetProperty("/api/usuarios",out _));
            Assert.Contains("\"persistAuthorization\":false",await http.GetStringAsync("/swagger/index.js"));
            Assert.Equal(HttpStatusCode.MovedPermanently,(await http.GetAsync("/swagger")).StatusCode);
        }
        Assert.Equal(HttpStatusCode.Unauthorized,(await http.GetAsync("/api/usuarios")).StatusCode);
        var emisor = new JwtEmisorToken(new() { Key=clave,Issuer="PuntoMarket.DemoLocal",Audience="PuntoMarket.Ventas.DemoLocal",ExpireMinutes=5 },new RelojSistema());
        http.DefaultRequestHeaders.Authorization=new("Bearer",emisor.Emitir(2,"cajero","Cajero Demo","Cajero",false));
        Assert.Equal(HttpStatusCode.Forbidden,(await http.GetAsync("/api/usuarios")).StatusCode);
        http.DefaultRequestHeaders.Authorization=new("Bearer","token-invalido");
        Assert.Equal(HttpStatusCode.Unauthorized,(await http.GetAsync("/api/usuarios")).StatusCode);
        await app.StopAsync();
    }
    [Fact]
    public void Token_emitido_es_compatible_con_claims_y_validacion_estricta_Demo()
    {
        var clave=Convert.ToHexString(RandomNumberGenerator.GetBytes(48));
        var emisor=new JwtEmisorToken(new() { Key=clave,Issuer="PuntoMarket.DemoLocal",Audience="PuntoMarket.Ventas.DemoLocal",ExpireMinutes=5 },new RelojSistema());
        var token=emisor.Emitir(1,"admin","Administrador Demo","Administrador",false);
        var opciones=new TokenValidationParameters {
            ValidateIssuer=true,ValidIssuer="PuntoMarket.DemoLocal",ValidateAudience=true,ValidAudience="PuntoMarket.Ventas.DemoLocal",
            ValidateIssuerSigningKey=true,IssuerSigningKey=new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave)),
            ValidateLifetime=true,RequireExpirationTime=true,RequireSignedTokens=true,ValidAlgorithms=[SecurityAlgorithms.HmacSha256],
            ClockSkew=TimeSpan.Zero,RoleClaimType="role",NameClaimType="unique_name"
        };
        var handler=new JwtSecurityTokenHandler { MapInboundClaims=false };
        var principal=handler.ValidateToken(token,opciones,out _);
        Assert.True(principal.IsInRole("Administrador")); Assert.Equal("1",principal.FindFirst("sub")?.Value);
        Assert.Equal("admin",principal.Identity?.Name); Assert.Equal("false",principal.FindFirst("debeCambiarPassword")?.Value);
        opciones.ValidAudience="audiencia-incorrecta";
        Assert.Throws<SecurityTokenInvalidAudienceException>(()=>handler.ValidateToken(token,opciones,out _));
        opciones.ValidAudience="PuntoMarket.Ventas.DemoLocal"; opciones.IssuerSigningKey=new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(48));
        Assert.ThrowsAny<SecurityTokenException>(()=>handler.ValidateToken(token,opciones,out _));
    }
}
