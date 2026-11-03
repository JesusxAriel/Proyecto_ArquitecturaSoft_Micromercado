using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using PuntoMarket.Reportes.Tests.Api;

namespace PuntoMarket.Reportes.Tests.VentasPorCategoria;

public class VentasPorCategoriaEndpointTests : IDisposable
{
    private const string Ruta = "/api/reportes/ventas-por-categoria";

    private readonly GeneradorDeTokenDePrueba _generador = new();
    private readonly WebApplicationFactory<Program> _fabrica;

    public VentasPorCategoriaEndpointTests()
    {
        _fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(constructor =>
        {
            constructor.UseSetting("Jwt:Key", _generador.Clave);
            constructor.UseSetting("Jwt:Issuer", GeneradorDeTokenDePrueba.Emisor);
            constructor.UseSetting("Jwt:Audience", GeneradorDeTokenDePrueba.Audiencia);
            constructor.UseSetting("ConnectionStrings:MySql", "Server=localhost;Database=base_de_prueba");
        });
    }

    private HttpClient CrearCliente(string? rol)
    {
        var cliente = _fabrica.CreateClient();
        if (rol is not null)
            cliente.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _generador.Generar(1, "ariana", rol));

        return cliente;
    }

    [Fact]
    public async Task Sin_token_responde_401()
    {
        var respuesta = await CrearCliente(null).GetAsync(Ruta);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Con_un_rol_distinto_de_administrador_responde_403()
    {
        var respuesta = await CrearCliente("Vendedor").GetAsync(Ruta);

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task Con_el_rango_invertido_responde_400()
    {
        var respuesta = await CrearCliente("Administrador").GetAsync($"{Ruta}?desde=2026-02-01&hasta=2026-01-01");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task Con_un_top_invalido_responde_400()
    {
        var respuesta = await CrearCliente("Administrador").GetAsync($"{Ruta}?top=0");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    public void Dispose() => _fabrica.Dispose();
}
