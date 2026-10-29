using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PuntoMarket.Reportes.Tests.Api;

public class JwtTests : IDisposable
{
    private readonly GeneradorDeTokenDePrueba _generador = new();
    private readonly WebApplicationFactory<Program> _fabrica;

    public JwtTests()
    {
        _fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(constructor =>
        {
            constructor.UseSetting("Jwt:Key", _generador.Clave);
            constructor.UseSetting("Jwt:Issuer", GeneradorDeTokenDePrueba.Emisor);
            constructor.UseSetting("Jwt:Audience", GeneradorDeTokenDePrueba.Audiencia);
        });
    }

    [Fact]
    public async Task Salud_responde_200_sin_token()
    {
        var respuesta = await _fabrica.CreateClient().GetAsync("/api/salud");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task Salud_protegido_responde_401_sin_token()
    {
        var respuesta = await _fabrica.CreateClient().GetAsync("/api/salud/protegido");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Salud_protegido_responde_200_con_token_valido()
    {
        var cliente = _fabrica.CreateClient();
        var token = _generador.Generar(7, "ariana");
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var respuesta = await cliente.GetAsync("/api/salud/protegido");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    public void Dispose() => _fabrica.Dispose();
}
