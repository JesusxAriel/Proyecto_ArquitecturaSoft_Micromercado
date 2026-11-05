using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;
namespace PuntoMarket.Reportes.Tests.Api;
public sealed class OpenApiTests
{
    private readonly GeneradorDeTokenDePrueba _tokens = new();
    private WebApplicationFactory<Program> Fabrica(string entorno) => new WebApplicationFactory<Program>().WithWebHostBuilder(b => {
        b.UseEnvironment(entorno);
        b.UseSetting("Jwt:Key", _tokens.Clave);
        b.UseSetting("Jwt:Issuer", GeneradorDeTokenDePrueba.Emisor);
        b.UseSetting("Jwt:Audience", GeneradorDeTokenDePrueba.Audiencia);
    });
    [Theory]
    [InlineData("Development")]
    [InlineData("LocalDemo")]
    public async Task Documentacion_local_publica_describe_contratos_y_conserva_JWT(string entorno)
    {
        using var fabrica = Fabrica(entorno);
        using var cliente = fabrica.CreateClient();
        Assert.NotNull(fabrica.Services.GetService<ISwaggerProvider>());
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/swagger")).StatusCode);
        Assert.Contains("swagger-ui-bundle.js", await cliente.GetStringAsync("/swagger/index.html"));
        Assert.Contains("\"persistAuthorization\":false", await cliente.GetStringAsync("/swagger/index.js"));
        using var json = JsonDocument.Parse(await cliente.GetStringAsync("/swagger/v1/swagger.json"));
        var root = json.RootElement;
        Assert.Equal("bearer", root.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer").GetProperty("scheme").GetString());
        var paths = root.GetProperty("paths");
        Assert.Equal(5, paths.EnumerateObject().Count());
        Assert.Equal(0, paths.GetProperty("/api/salud").GetProperty("get").GetProperty("security").GetArrayLength());
        foreach (var (ruta, filtros) in new[] {
            ("/api/reportes/ventas", new[] {"desde","hasta","estado","cliente","tope","formato"}),
            ("/api/reportes/ventas-por-categoria", new[] {"desde","hasta","idcategoria","top","formato"}) })
        {
            var op = paths.GetProperty(ruta).GetProperty("get");
            var nombres = op.GetProperty("parameters").EnumerateArray().Select(x => x.GetProperty("name").GetString()!.ToLowerInvariant()).ToArray();
            foreach (var filtro in filtros) Assert.Contains(filtro, nombres);
            var contenido = op.GetProperty("responses").GetProperty("200").GetProperty("content");
            foreach (var tipo in new[] {"application/json","application/pdf","application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"}) Assert.True(contenido.TryGetProperty(tipo, out _));
            Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync(ruta)).StatusCode);
            cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _tokens.Generar(2,"cajero","Cajero"));
            Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync(ruta)).StatusCode);
            cliente.DefaultRequestHeaders.Authorization = null;
        }
    }
    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData("Testing")]
    public async Task Otros_entornos_no_registran_ni_exponen_documentacion(string entorno)
    {
        using var fabrica = Fabrica(entorno);
        using var cliente = fabrica.CreateClient();
        Assert.Null(fabrica.Services.GetService<ISwaggerProvider>());
        foreach (var ruta in new[] {"/swagger","/swagger/index.html","/swagger/v1/swagger.json","/swagger/swagger-ui-bundle.js"})
            Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync(ruta)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/api/reportes/ventas")).StatusCode);
    }
}
