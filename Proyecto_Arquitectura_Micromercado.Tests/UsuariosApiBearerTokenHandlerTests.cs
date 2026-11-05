using System.Net;
using System.Net.Http.Headers;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Proyecto_Arquitectura_Micromercado.Dtos.Usuarios;
using Proyecto_Arquitectura_Micromercado.Pages.Shared;
using Proyecto_Arquitectura_Micromercado.Servicios;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public sealed class UsuariosApiBearerTokenHandlerTests
{
    [Fact]
    public async Task Agrega_bearer_si_hay_jwt_en_sesion()
    {
        var (httpClient, servidor, _) = CrearHttpClient("jwt-de-prueba");
        using (httpClient)

        using (var response = await httpClient.GetAsync("api/usuarios"))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Bearer", servidor.Authorization?.Scheme);
            Assert.Equal("jwt-de-prueba", servidor.Authorization?.Parameter);
        }
    }

    [Fact]
    public async Task No_agrega_authorization_si_no_hay_sesion()
    {
        var (httpClient, servidor, _) = CrearHttpClient(null, crearSesion: false);
        using (httpClient)
        using (var response = await httpClient.GetAsync("api/usuarios"))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Null(servidor.Authorization);
        }
    }

    [Fact]
    public async Task No_agrega_authorization_si_el_jwt_esta_vacio()
    {
        var (httpClient, servidor, _) = CrearHttpClient(string.Empty);
        using (httpClient)
        using (var response = await httpClient.GetAsync("api/usuarios"))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Null(servidor.Authorization);
        }
    }

    [Fact]
    public async Task No_sobrescribe_authorization_configurada_en_la_solicitud()
    {
        var (httpClient, servidor, _) = CrearHttpClient("jwt-de-sesion");
        using (httpClient)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "api/usuarios");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", "credencial-explicita");
            using var response = await httpClient.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Basic", servidor.Authorization?.Scheme);
            Assert.Equal("credencial-explicita", servidor.Authorization?.Parameter);
        }
    }

    [Fact]
    public async Task Login_sin_jwt_en_sesion_envia_credenciales_sin_authorization()
    {
        var (httpClient, servidor, accessor) = CrearHttpClient(null);
        using (httpClient)
        {
            var cliente = new UsuariosApiClient(httpClient);
            var respuesta = await cliente.IniciarSesionAsync(
                new IniciarSesionDto("ana@ejemplo.com", "Clave123!"));

            Assert.Equal("jwt-respuesta", respuesta.Token);
            Assert.Null(servidor.Authorization);
            using var body = JsonDocument.Parse(servidor.Cuerpo!);
            Assert.Equal("ana@ejemplo.com", body.RootElement.GetProperty("correo").GetString());
            Assert.Equal("Clave123!", body.RootElement.GetProperty("contrasena").GetString());
            Assert.False(accessor.HttpContext!.User.Identity!.IsAuthenticated);
        }
    }

    private static (HttpClient Client, ServidorHttp Servidor, HttpContextAccessor Accessor)
        CrearHttpClient(string? token, bool crearSesion = true)
    {
        var servidor = new ServidorHttp();
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        if (crearSesion)
        {
            var session = new SesionPrueba();
            if (token is not null)
                session.Set(VariablesDeSesion.Jwt, Encoding.UTF8.GetBytes(token));
            accessor.HttpContext.Features.Set<ISessionFeature>(new SessionFeature { Session = session });
        }

        var handler = new UsuariosApiBearerTokenHandler(accessor)
        {
            InnerHandler = servidor
        };
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://usuarios.test/")
        };
        return (client, servidor, accessor);
    }

    private sealed class ServidorHttp : HttpMessageHandler
    {
        public AuthenticationHeaderValue? Authorization { get; private set; }
        public string? Cuerpo { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Authorization = request.Headers.Authorization;
            Cuerpo = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            var contenido = request.RequestUri?.AbsolutePath == "/api/autenticacion/login"
                ? """{"token":"jwt-respuesta"}"""
                : "{}";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(contenido, Encoding.UTF8, "application/json")
            };
        }
    }

    private sealed class SesionPrueba : ISession
    {
        private readonly Dictionary<string, byte[]> valores = new(StringComparer.Ordinal);

        public bool IsAvailable => true;
        public string Id => "sesion-prueba";
        public IEnumerable<string> Keys => valores.Keys;
        public void Clear() => valores.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => valores.Remove(key);
        public void Set(string key, byte[] value) => valores[key] = value;
        public bool TryGetValue(string key, [NotNullWhen(true)] out byte[]? value) =>
            valores.TryGetValue(key, out value);
    }

    private sealed class SessionFeature : ISessionFeature
    {
        public ISession Session { get; set; } = null!;
    }
}
