using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PuntoMarket.Ventas.Api;
using Swashbuckle.AspNetCore.Swagger;

namespace PuntoMarket.Ventas.Tests.Api;

public sealed class OpenApiHttpTests
{
    [Theory]
    [InlineData("Development")]
    [InlineData("LocalDemo")]
    public async Task Entorno_local_genera_UI_documento_Bearer_y_contratos_sin_MySQL(string ambiente)
    {
        await using var host = await Host.Crear(ambiente);
        Assert.NotNull(host.App.Services.GetService<ISwaggerProvider>());
        Assert.Equal(HttpStatusCode.OK, (await host.Client.GetAsync("/swagger")).StatusCode);
        var ui = await host.Client.GetStringAsync("/swagger/index.html");
        Assert.Contains("swagger-ui-bundle.js", ui);
        var inicializador = await host.Client.GetStringAsync("/swagger/index.js");
        Assert.Contains("SwaggerUIBundle", inicializador);
        Assert.Contains("v1/swagger.json", inicializador);
        Assert.Contains("\"persistAuthorization\":false", inicializador);
        using var response = await host.Client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        var bearer = root.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
        Assert.Equal("http", bearer.GetProperty("type").GetString());
        Assert.Equal("bearer", bearer.GetProperty("scheme").GetString());
        Assert.Contains("Razor", root.GetProperty("info").GetProperty("description").GetString());
        var paths = root.GetProperty("paths");
        foreach (var path in new[] { "/api/productos", "/api/productos/{id}", "/api/productos/{id}/precio", "/api/productos/{id}/stock",
            "/api/clientes", "/api/ventas", "/api/ventas/{id}", "/api/ventas/total-dia", "/api/ventas/{id}/anular",
            "/api/ventas/{id}/comprobante", "/api/ventas/{id}/comprobante/html", "/health", "/images/logo.jpg" })
            Assert.True(paths.TryGetProperty(path, out _), path);
        var tags = paths.EnumerateObject().SelectMany(p => p.Value.EnumerateObject())
            .Where(p => p.Value.TryGetProperty("tags", out _))
            .SelectMany(p => p.Value.GetProperty("tags").EnumerateArray()).Select(t => t.GetString()).ToArray();
        foreach (var tag in new[] { "Productos", "Clientes", "Ventas", "Comprobantes", "Health" }) Assert.Contains(tag, tags);
        var venta = paths.GetProperty("/api/ventas").GetProperty("post");
        Assert.True(venta.TryGetProperty("requestBody", out _));
        foreach (var status in new[] { "201", "400", "401", "403", "404", "409", "503" })
            Assert.True(venta.GetProperty("responses").TryGetProperty(status, out _), status);
        var anulacion = paths.GetProperty("/api/ventas/{id}/anular").GetProperty("post");
        Assert.Contains("Solo Administrador", anulacion.GetProperty("description").GetString());
        Assert.True(anulacion.GetProperty("responses").TryGetProperty("204", out _));
        Assert.True(paths.GetProperty("/api/ventas/{id}/comprobante/html").GetProperty("get")
            .GetProperty("responses").GetProperty("200").GetProperty("content").TryGetProperty("text/html", out _));
        Assert.Equal(0, paths.GetProperty("/health").GetProperty("get").GetProperty("security").GetArrayLength());
        Assert.True(root.GetProperty("security")[0].TryGetProperty("Bearer", out _));
        Assert.Equal(HttpStatusCode.OK, (await host.Client.GetAsync("/health")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await host.Client.GetAsync("/api/ventas")).StatusCode);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData("Testing")]
    public async Task Fuera_de_entornos_locales_no_registra_ni_expone_Swagger(string ambiente)
    {
        await using var host = await Host.Crear(ambiente);
        Assert.Null(host.App.Services.GetService<ISwaggerProvider>());
        foreach (var ruta in new[] { "/swagger", "/swagger/index.html", "/swagger/v1/swagger.json", "/swagger/swagger-ui-bundle.js" })
        {
            using var response = await host.Client.GetAsync(ruta);
            // La política fallback exige JWT incluso para rutas inexistentes.
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.DoesNotContain("SwaggerUIBundle", await response.Content.ReadAsStringAsync());
        }
    }

    private sealed class Host(WebApplication app, HttpClient client) : IAsyncDisposable
    {
        public WebApplication App => app;
        public HttpClient Client => client;
        public static async Task<Host> Crear(string ambiente)
        {
            var app = ApiBootstrap.Crear(["--urls", "http://127.0.0.1:0", "--environment", ambiente], builder =>
            {
                builder.Logging.ClearProviders();
                builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = null, ["Jwt:Issuer"] = null, ["Jwt:Audience"] = null,
                    ["ConnectionStrings:Ventas"] = null
                });
            });
            await app.StartAsync();
            var direccion = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            return new Host(app, new HttpClient { BaseAddress = new Uri(direccion), Timeout = TimeSpan.FromSeconds(20) });
        }
        public async ValueTask DisposeAsync() { client.Dispose(); await app.StopAsync(); await app.DisposeAsync(); }
    }
}
