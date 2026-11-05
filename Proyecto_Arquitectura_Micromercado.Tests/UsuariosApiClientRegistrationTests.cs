using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Proyecto_Arquitectura_Micromercado.Servicios;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public sealed class UsuariosApiClientRegistrationTests
{
    [Fact]
    public void Registra_el_cliente_tipado_con_la_url_configurada_y_agrega_barra_final()
    {
        var configuration = CrearConfiguracion("https://usuarios.ejemplo.com/api");
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddUsuariosApiClient(configuration);

        using var provider = services.BuildServiceProvider();
        Assert.IsType<UsuariosApiClient>(provider.GetRequiredService<UsuariosApiClient>());
        using var httpClient = provider.GetRequiredService<IHttpClientFactory>()
            .CreateClient(nameof(UsuariosApiClient));
        Assert.Equal(new Uri("https://usuarios.ejemplo.com/api/"), httpClient.BaseAddress);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("usuarios:8080")]
    [InlineData("ftp://usuarios:8080/")]
    [InlineData("http://usuario:clave@usuarios:8080/")]
    [InlineData("http://usuarios:8080/?version=1")]
    [InlineData("http://usuarios:8080/#fragmento")]
    public void Rechaza_una_url_base_faltante_o_invalida(string? baseUrl)
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(
            () => services.AddUsuariosApiClient(CrearConfiguracion(baseUrl)));

        Assert.Contains("UsuariosApi:BaseUrl", exception.Message);
    }

    private static IConfiguration CrearConfiguracion(string? baseUrl) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UsuariosApi:BaseUrl"] = baseUrl
            })
            .Build();
}
