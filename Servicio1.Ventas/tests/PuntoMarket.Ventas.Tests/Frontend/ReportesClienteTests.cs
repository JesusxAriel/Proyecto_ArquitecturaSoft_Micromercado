using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using PuntoMarket.Ventas.Frontend.Clients;
using PuntoMarket.Ventas.Frontend.Security;
namespace PuntoMarket.Ventas.Tests.Frontend;
public sealed class ReportesClienteTests
{
    [Theory]
    [InlineData(401)] [InlineData(403)] [InlineData(500)] [InlineData(400)]
    public async Task Conserva_errores_de_API(int estado)
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var api = Crear(cache, new Handler(_ => new HttpResponseMessage((HttpStatusCode)estado)));
        var error = await Assert.ThrowsAsync<ReportesApiException>(() => api.Listado(new(), default));
        Assert.Equal(estado, error.Estado);
    }
    [Fact]
    public async Task Envia_JWT_solo_en_cabecera_y_codifica_filtros_en_descarga()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var api = Crear(cache, new Handler(r => {
            Assert.Equal("Bearer", r.Headers.Authorization?.Scheme);
            Assert.NotNull(r.Headers.Authorization?.Parameter);
            Assert.DoesNotContain(r.Headers.Authorization!.Parameter!, r.RequestUri!.ToString());
            Assert.Equal("/api/reportes/ventas", r.RequestUri.AbsolutePath);
            Assert.Contains("cliente=A%26B", r.RequestUri.Query);
            var respuesta = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([37,80,68,70]) };
            respuesta.Content.Headers.ContentType = new("application/pdf"); return respuesta;
        }));
        var archivo = await api.DescargarListado(new() { Cliente = "A&B" }, "pdf", default);
        Assert.Equal("application/pdf", archivo.TipoContenido);
        Assert.Equal(new byte[] {37,80,68,70}, archivo.Contenido);
    }
    [Fact]
    public async Task API_no_disponible_devuelve_error_controlado()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var api = Crear(cache, new Handler(_ => throw new HttpRequestException()));
        var error = await Assert.ThrowsAsync<ReportesApiException>(() => api.Categorias(new(), default));
        Assert.Equal(503, error.Estado);
    }
    private static ReportesApiClient Crear(MemoryCache cache, HttpMessageHandler handler)
    {
        var clave=Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48));
        var sesiones=SesionRealPrueba.Sesiones(cache,clave);var id=sesiones.Crear(SesionRealPrueba.Token(clave,"Administrador"));
        var contexto=SesionRealPrueba.Contexto(sesiones,id);
        return new(new HttpClient(handler) { BaseAddress = new("http://localhost:5012/") },new ResolucionSesion(new HttpContextAccessor {HttpContext=contexto},sesiones));
    }
    private sealed class Handler(Func<HttpRequestMessage,HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => Task.FromResult(responder(request));
    }
}
