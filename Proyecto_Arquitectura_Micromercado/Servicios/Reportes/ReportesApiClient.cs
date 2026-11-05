using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Proyecto_Arquitectura_Micromercado.Dtos.Reportes;

namespace Proyecto_Arquitectura_Micromercado.Servicios.Reportes;

public sealed class ReportesApiClient(HttpClient http, ITokenDeAcceso tokenDeAcceso) : IReportesApiClient
{
    public const string RutaVentasPorCategoria = "api/reportes/ventas-por-categoria";
    public const string RutaListadoVentas = "api/reportes/ventas";

    private const string MensajeNoConfigurado = "El servicio de reportes no está configurado.";
    private const string MensajeNoDisponible =
        "El servicio de reportes no está disponible. Intente nuevamente en unos minutos.";
    private const string MensajeSesion =
        "Su sesión con el servicio de reportes no es válida o venció. Inicie sesión nuevamente.";
    private const string MensajePermisos = "No tiene permisos para consultar este reporte.";
    private const string MensajeFiltros =
        "Los filtros indicados no son válidos. Revise las fechas y el valor de «Top».";
    private const string MensajeGeneracion =
        "El servicio de reportes no pudo generar el reporte. Intente nuevamente.";
    private const string NombreDeArchivoPorDefecto = "ventas-por-categoria";
    private const string MensajeFiltrosDeListado =
        "Los filtros indicados no son válidos. Revise las fechas, el estado, el cliente y el tope de filas.";
    private const string NombreDeArchivoDeListadoPorDefecto = "listado-de-ventas";

    public async Task<RespuestaVentasPorCategoriaDto> ObtenerVentasPorCategoriaAsync(
        FiltrosVentasPorCategoriaDto filtros,
        CancellationToken cancellationToken)
    {
        using var respuesta = await EnviarAsync(filtros, "json", cancellationToken);

        return await LeerJsonAsync(respuesta, cancellationToken);
    }

    public async Task<ArchivoDescargadoDto> DescargarVentasPorCategoriaAsync(
        FiltrosVentasPorCategoriaDto filtros,
        FormatoDeDescarga formato,
        CancellationToken cancellationToken)
    {
        var extension = formato == FormatoDeDescarga.Pdf ? "pdf" : "xlsx";
        using var respuesta = await EnviarAsync(filtros, extension, cancellationToken);
        var contenido = await respuesta.Content.ReadAsByteArrayAsync(cancellationToken);

        return new ArchivoDescargadoDto(
            contenido,
            respuesta.Content.Headers.ContentType?.MediaType ?? "application/octet-stream",
            NombreDeArchivo(respuesta, extension));
    }

    public static string ConstruirRuta(FiltrosVentasPorCategoriaDto filtros, string formato)
    {
        var parametros = new List<string>();
        Agregar(parametros, "desde", filtros.Desde?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        Agregar(parametros, "hasta", filtros.Hasta?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        Agregar(parametros, "idCategoria", filtros.IdCategoria?.ToString(CultureInfo.InvariantCulture));
        Agregar(parametros, "top", filtros.Top?.ToString(CultureInfo.InvariantCulture));
        Agregar(parametros, "formato", formato);

        return $"{RutaVentasPorCategoria}?{string.Join('&', parametros)}";
    }

    public async Task<RespuestaListadoVentasDto> ObtenerListadoDeVentasAsync(
        FiltrosListadoVentasDto filtros,
        CancellationToken cancellationToken)
    {
        using var respuesta = await EnviarListadoAsync(filtros, "json", cancellationToken);

        return await LeerListadoAsync(respuesta, cancellationToken);
    }

    public async Task<ArchivoDescargadoDto> DescargarListadoDeVentasAsync(
        FiltrosListadoVentasDto filtros,
        FormatoDeDescarga formato,
        CancellationToken cancellationToken)
    {
        var extension = formato == FormatoDeDescarga.Pdf ? "pdf" : "xlsx";
        using var respuesta = await EnviarListadoAsync(filtros, extension, cancellationToken);
        var contenido = await respuesta.Content.ReadAsByteArrayAsync(cancellationToken);

        return new ArchivoDescargadoDto(
            contenido,
            respuesta.Content.Headers.ContentType?.MediaType ?? "application/octet-stream",
            NombreDeArchivoDeListado(respuesta, extension));
    }

    public static string ConstruirRutaDeListado(FiltrosListadoVentasDto filtros, string formato)
    {
        var parametros = new List<string>();
        Agregar(parametros, "desde", filtros.Desde?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        Agregar(parametros, "hasta", filtros.Hasta?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        Agregar(parametros, "estado", filtros.Estado);
        Agregar(parametros, "cliente", filtros.Cliente);
        Agregar(parametros, "tope", filtros.Tope?.ToString(CultureInfo.InvariantCulture));
        Agregar(parametros, "formato", formato);

        return $"{RutaListadoVentas}?{string.Join('&', parametros)}";
    }

    private async Task<HttpResponseMessage> EnviarListadoAsync(
        FiltrosListadoVentasDto filtros,
        string formato,
        CancellationToken cancellationToken)
    {
        using var peticion = CrearPeticion(ConstruirRutaDeListado(filtros, formato));
        var respuesta = await EjecutarAsync(peticion, cancellationToken);
        if (respuesta.IsSuccessStatusCode)
            return respuesta;

        var mensaje = await MensajeDelListadoAsync(respuesta, cancellationToken);
        respuesta.Dispose();
        throw new ReporteNoDisponibleException(mensaje);
    }

    private static async Task<string> MensajeDelListadoAsync(
        HttpResponseMessage respuesta,
        CancellationToken cancellationToken)
    {
        if (respuesta.StatusCode != HttpStatusCode.BadRequest)
            return MensajeParaEstado(respuesta.StatusCode);

        return await LeerDetalleAsync(respuesta, cancellationToken) ?? MensajeFiltrosDeListado;
    }

    private static async Task<string?> LeerDetalleAsync(
        HttpResponseMessage respuesta,
        CancellationToken cancellationToken)
    {
        try
        {
            var problema = await respuesta.Content.ReadFromJsonAsync<ProblemaDeApiDto>(
                JsonSerializerOptions.Web,
                cancellationToken);

            return string.IsNullOrWhiteSpace(problema?.Detail) ? null : problema.Detail;
        }
        catch (Exception excepcion) when (excepcion is JsonException or NotSupportedException)
        {
            return null;
        }
    }

    private static async Task<RespuestaListadoVentasDto> LeerListadoAsync(
        HttpResponseMessage respuesta,
        CancellationToken cancellationToken)
    {
        try
        {
            return await respuesta.Content.ReadFromJsonAsync<RespuestaListadoVentasDto>(
                       JsonSerializerOptions.Web,
                       cancellationToken)
                   ?? throw new ReporteNoDisponibleException(MensajeGeneracion);
        }
        catch (JsonException excepcion)
        {
            throw new ReporteNoDisponibleException(MensajeGeneracion, excepcion);
        }
    }

    private static string NombreDeArchivoDeListado(HttpResponseMessage respuesta, string extension)
    {
        var disposicion = respuesta.Content.Headers.ContentDisposition;
        var nombre = disposicion?.FileNameStar ?? disposicion?.FileName;

        return string.IsNullOrWhiteSpace(nombre)
            ? $"{NombreDeArchivoDeListadoPorDefecto}.{extension}"
            : nombre.Trim('"');
    }

    private async Task<HttpResponseMessage> EnviarAsync(
        FiltrosVentasPorCategoriaDto filtros,
        string formato,
        CancellationToken cancellationToken)
    {
        using var peticion = CrearPeticion(ConstruirRuta(filtros, formato));
        var respuesta = await EjecutarAsync(peticion, cancellationToken);
        if (respuesta.IsSuccessStatusCode)
            return respuesta;

        respuesta.Dispose();
        throw new ReporteNoDisponibleException(MensajeParaEstado(respuesta.StatusCode));
    }

    private HttpRequestMessage CrearPeticion(string ruta)
    {
        if (http.BaseAddress is null)
            throw new ReporteNoDisponibleException(MensajeNoConfigurado);

        var peticion = new HttpRequestMessage(HttpMethod.Get, ruta);
        peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ObtenerToken());
        return peticion;
    }

    private string ObtenerToken()
    {
        try
        {
            return tokenDeAcceso.Obtener();
        }
        catch (TokenDeAccesoNoDisponibleException excepcion)
        {
            throw new ReporteNoDisponibleException(MensajeSesion, excepcion);
        }
    }

    private async Task<HttpResponseMessage> EjecutarAsync(
        HttpRequestMessage peticion,
        CancellationToken cancellationToken)
    {
        try
        {
            return await http.SendAsync(peticion, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (Exception excepcion) when (excepcion is HttpRequestException or TaskCanceledException
            && !cancellationToken.IsCancellationRequested)
        {
            throw new ReporteNoDisponibleException(MensajeNoDisponible, excepcion);
        }
    }

    private static async Task<RespuestaVentasPorCategoriaDto> LeerJsonAsync(
        HttpResponseMessage respuesta,
        CancellationToken cancellationToken)
    {
        try
        {
            return await respuesta.Content.ReadFromJsonAsync<RespuestaVentasPorCategoriaDto>(
                       JsonSerializerOptions.Web,
                       cancellationToken)
                   ?? throw new ReporteNoDisponibleException(MensajeGeneracion);
        }
        catch (JsonException excepcion)
        {
            throw new ReporteNoDisponibleException(MensajeGeneracion, excepcion);
        }
    }

    private static string MensajeParaEstado(HttpStatusCode estado) => estado switch
    {
        HttpStatusCode.Unauthorized => MensajeSesion,
        HttpStatusCode.Forbidden => MensajePermisos,
        HttpStatusCode.BadRequest => MensajeFiltros,
        HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout => MensajeNoDisponible,
        _ => MensajeGeneracion
    };

    private static string NombreDeArchivo(HttpResponseMessage respuesta, string extension)
    {
        var disposicion = respuesta.Content.Headers.ContentDisposition;
        var nombre = disposicion?.FileNameStar ?? disposicion?.FileName;

        return string.IsNullOrWhiteSpace(nombre)
            ? $"{NombreDeArchivoPorDefecto}.{extension}"
            : nombre.Trim('"');
    }

    private static void Agregar(List<string> parametros, string nombre, string? valor)
    {
        if (valor is not null)
            parametros.Add($"{nombre}={Uri.EscapeDataString(valor)}");
    }
}
