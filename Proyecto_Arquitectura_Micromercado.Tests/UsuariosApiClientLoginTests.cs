using System.Net;
using System.Text;
using System.Text.Json;
using Proyecto_Arquitectura_Micromercado.Dtos.Usuarios;
using Proyecto_Arquitectura_Micromercado.Servicios;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public sealed class UsuariosApiClientLoginTests
{
    [Fact]
    public async Task IniciarSesion_envia_el_contrato_y_devuelve_el_token()
    {
        var handler = new LoginHandler(_ => Respuesta(HttpStatusCode.OK, """{"token":"jwt-valido"}"""));
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://usuarios.test/") };
        var cliente = new UsuariosApiClient(httpClient);

        var respuesta = await cliente.IniciarSesionAsync(
            new IniciarSesionDto("ana@ejemplo.com", "Clave123!"));

        Assert.Equal("jwt-valido", respuesta.Token);
        Assert.Equal("POST", handler.Metodo);
        Assert.Equal("/api/autenticacion/login", handler.Ruta);
        using var cuerpo = JsonDocument.Parse(handler.Cuerpo!);
        Assert.Equal("ana@ejemplo.com", cuerpo.RootElement.GetProperty("correo").GetString());
        Assert.Equal("Clave123!", cuerpo.RootElement.GetProperty("contrasena").GetString());
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "Ingrese un correo válido y una contraseña.")]
    [InlineData(HttpStatusCode.Unauthorized, "Correo o contraseña incorrectos.")]
    [InlineData(HttpStatusCode.InternalServerError, "No se pudo completar el inicio de sesión con el servicio de usuarios. Intente nuevamente.")]
    public async Task IniciarSesion_distingue_errores_http(HttpStatusCode statusCode, string mensajeEsperado)
    {
        var handler = new LoginHandler(_ => Respuesta(statusCode, """{"title":"detalle interno"}"""));
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://usuarios.test/") };
        var cliente = new UsuariosApiClient(httpClient);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => cliente.IniciarSesionAsync(new IniciarSesionDto("ana@ejemplo.com", "Clave123!")));

        Assert.Equal(statusCode, exception.StatusCode);
        Assert.Equal(mensajeEsperado, exception.Message);
        Assert.DoesNotContain("detalle interno", exception.Message);
    }

    [Fact]
    public async Task IniciarSesion_identifica_falla_de_conexion_como_indisponibilidad()
    {
        var handler = new LoginHandler(_ => throw new HttpRequestException("fallo de conexión"));
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://usuarios.test/") };
        var cliente = new UsuariosApiClient(httpClient);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => cliente.IniciarSesionAsync(new IniciarSesionDto("ana@ejemplo.com", "Clave123!")));

        Assert.Null(exception.StatusCode);
        Assert.Contains("No se pudo conectar", exception.Message);
        Assert.IsType<HttpRequestException>(exception.InnerException);
    }

    [Theory]
    [InlineData("{\"token\":\"\"}")]
    [InlineData("{}")]
    [InlineData("null")]
    public async Task IniciarSesion_rechaza_respuestas_sin_token(string json)
    {
        var handler = new LoginHandler(_ => Respuesta(HttpStatusCode.OK, json));
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://usuarios.test/") };
        var cliente = new UsuariosApiClient(httpClient);

        await Assert.ThrowsAsync<JsonException>(
            () => cliente.IniciarSesionAsync(new IniciarSesionDto("ana@ejemplo.com", "Clave123!")));
    }

    private static HttpResponseMessage Respuesta(HttpStatusCode statusCode, string json) =>
        new(statusCode) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private sealed class LoginHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public string? Metodo { get; private set; }
        public string? Ruta { get; private set; }
        public string? Cuerpo { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Metodo = request.Method.Method;
            Ruta = request.RequestUri?.AbsolutePath;
            Cuerpo = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return responder(request);
        }
    }
}
