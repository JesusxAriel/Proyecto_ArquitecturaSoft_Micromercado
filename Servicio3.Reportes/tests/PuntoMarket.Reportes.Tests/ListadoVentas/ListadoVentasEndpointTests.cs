using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using PuntoMarket.Reportes.Application.Ports.Consultas;
using PuntoMarket.Reportes.Domain.ListadoVentas;
using PuntoMarket.Reportes.Tests.Api;

namespace PuntoMarket.Reportes.Tests.ListadoVentas;

public class ListadoVentasEndpointTests : IDisposable
{
    private const string Ruta = "/api/reportes/ventas";

    private readonly GeneradorDeTokenDePrueba _generador = new();
    private readonly WebApplicationFactory<Program> _fabrica;

    public ListadoVentasEndpointTests()
    {
        _fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(constructor =>
        {
            constructor.UseSetting("Jwt:Key", _generador.Clave);
            constructor.UseSetting("Jwt:Issuer", GeneradorDeTokenDePrueba.Emisor);
            constructor.UseSetting("Jwt:Audience", GeneradorDeTokenDePrueba.Audiencia);
            constructor.UseSetting("ConnectionStrings:MySql", "Server=localhost;Database=base_de_prueba");
            constructor.ConfigureTestServices(servicios =>
                servicios.AddScoped<IConsultaListadoVentas>(_ => ConsultaConDosVentas()));
        });
    }

    private static ConsultaFalsa ConsultaConDosVentas() =>
        new(
            new FilaDeVenta(1, "F-0001", new DateTime(2026, 1, 15, 10, 0, 0), "1234567", "Comercial Andina", "ventas01", EstadoDeVenta.Completada, 120m),
            new FilaDeVenta(2, "F-0002", new DateTime(2026, 1, 14, 16, 30, 0), "7654321", "Panadería El Trigal", "ventas02", EstadoDeVenta.Anulada, 45m));

    private HttpClient CrearCliente(string? rol)
    {
        var cliente = _fabrica.CreateClient();
        if (rol is not null)
            cliente.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _generador.Generar(3, "alejandra", rol));

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
        var respuesta = await CrearCliente("Cajero").GetAsync(Ruta);

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task Como_administrador_el_formato_json_devuelve_las_filas_y_los_totales()
    {
        var respuesta = await CrearCliente("Administrador").GetAsync($"{Ruta}?formato=json");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("application/json", respuesta.Content.Headers.ContentType?.MediaType);

        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        Assert.Contains("\"comprobante\":\"F-0001\"", cuerpo);
        Assert.Contains("\"estado\":\"ANULADA\"", cuerpo);
        Assert.Contains("\"generadoPor\":\"alejandra\"", cuerpo);
        Assert.Contains("\"ventasCompletadas\":1", cuerpo);
    }

    [Fact]
    public async Task Sin_formato_responde_json()
    {
        var respuesta = await CrearCliente("Administrador").GetAsync(Ruta);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("application/json", respuesta.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Como_administrador_el_formato_pdf_devuelve_el_archivo()
    {
        var respuesta = await CrearCliente("Administrador").GetAsync($"{Ruta}?formato=pdf");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("application/pdf", respuesta.Content.Headers.ContentType?.MediaType);
        Assert.Contains("listado-de-ventas-", respuesta.Content.Headers.ContentDisposition?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task Como_administrador_el_formato_xlsx_devuelve_el_archivo()
    {
        var respuesta = await CrearCliente("Administrador").GetAsync($"{Ruta}?formato=xlsx");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            respuesta.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Los_filtros_llegan_al_reporte()
    {
        var respuesta = await CrearCliente("Administrador")
            .GetAsync($"{Ruta}?desde=2026-01-01&hasta=2026-01-31&estado=COMPLETADA&cliente=Andina&tope=25");

        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("\"estado\":\"COMPLETADA\"", cuerpo);
        Assert.Contains("\"cliente\":\"Andina\"", cuerpo);
        Assert.Contains("\"tope\":25", cuerpo);
    }

    [Theory]
    [InlineData("?desde=2026-02-01&hasta=2026-01-01")]
    [InlineData("?tope=0")]
    [InlineData("?tope=5000")]
    [InlineData("?cliente=A")]
    [InlineData("?estado=PENDIENTE")]
    [InlineData("?formato=docx")]
    public async Task Un_filtro_invalido_responde_400_en_espanol(string consulta)
    {
        var respuesta = await CrearCliente("Administrador").GetAsync($"{Ruta}{consulta}");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Contains("\"detail\"", await respuesta.Content.ReadAsStringAsync());
    }

    public void Dispose() => _fabrica.Dispose();
}
