using System.Net;
using System.Text;
using Proyecto_Arquitectura_Micromercado.Dtos.Reportes;
using Proyecto_Arquitectura_Micromercado.Servicios.Reportes;

namespace Proyecto_Arquitectura_Micromercado.Tests;

internal sealed class ManejadorHttpFalso : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> respuesta;

    public ManejadorHttpFalso(Func<HttpRequestMessage, HttpResponseMessage> respuesta)
    {
        this.respuesta = respuesta;
    }

    public ManejadorHttpFalso(HttpStatusCode estado, string contenido = "")
        : this(_ => RespuestaDeTexto(estado, contenido))
    {
    }

    public HttpRequestMessage? Peticion { get; private set; }

    public static HttpResponseMessage RespuestaDeTexto(HttpStatusCode estado, string contenido) =>
        new(estado) { Content = new StringContent(contenido, Encoding.UTF8, "application/json") };

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Peticion = request;
        return Task.FromResult(respuesta(request));
    }
}

internal sealed class TokenDeAccesoFalso(string? token = "token-de-prueba") : ITokenDeAcceso
{
    public string Obtener() => token ?? throw new TokenDeAccesoNoDisponibleException();
}

internal sealed class ClienteDeReportesFalso : IReportesApiClient
{
    public int Consultas { get; private set; }

    public int Descargas { get; private set; }

    public FiltrosVentasPorCategoriaDto? FiltrosRecibidos { get; private set; }

    public FormatoDeDescarga? FormatoRecibido { get; private set; }

    public Exception? Error { get; init; }

    public RespuestaVentasPorCategoriaDto Respuesta { get; init; } = new(
        [new FilaVentasPorCategoriaDto(1, "Lácteos", 5, 120.5m)],
        new TotalesVentasPorCategoriaDto(5, 120.5m, "Lácteos"),
        new DateTime(2026, 5, 20, 14, 30, 0),
        "admin");

    public Task<RespuestaVentasPorCategoriaDto> ObtenerVentasPorCategoriaAsync(
        FiltrosVentasPorCategoriaDto filtros,
        CancellationToken cancellationToken)
    {
        Consultas++;
        FiltrosRecibidos = filtros;
        return Error is null ? Task.FromResult(Respuesta) : Task.FromException<RespuestaVentasPorCategoriaDto>(Error);
    }

    public Task<ArchivoDescargadoDto> DescargarVentasPorCategoriaAsync(
        FiltrosVentasPorCategoriaDto filtros,
        FormatoDeDescarga formato,
        CancellationToken cancellationToken)
    {
        Descargas++;
        FiltrosRecibidos = filtros;
        FormatoRecibido = formato;
        return Error is null
            ? Task.FromResult(new ArchivoDescargadoDto([1, 2, 3], "application/pdf", "reporte.pdf"))
            : Task.FromException<ArchivoDescargadoDto>(Error);
    }
}
