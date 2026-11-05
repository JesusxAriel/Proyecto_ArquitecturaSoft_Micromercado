using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using PuntoMarket.Ventas.Frontend.Security;
namespace PuntoMarket.Ventas.Frontend.Clients;

public sealed class ReportesApiException(int estado) : Exception(MensajePorEstado(estado))
{
    public int Estado { get; } = estado;
    private static string MensajePorEstado(int estado) => estado switch {
        400 => "Revise las fechas y los filtros del reporte.",
        401 => "La sesión no es válida. Vuelva a iniciar sesión.",
        403 => "Solo Administrador puede consultar reportes.",
        500 => "Reportes no pudo generar el resultado. Compruebe su conexión de lectura.",
        _ => "La API Reportes no está disponible. Intente nuevamente más tarde."
    };
}
public sealed class ReportesApiClient(HttpClient http, ResolucionSesion resolucion) : IReportesApiClient
{
    private static Dictionary<string,string?> Fechas(FiltroFechasReporte f) => new() { ["desde"] = f.Desde?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), ["hasta"] = f.Hasta?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) };
    private static Dictionary<string,string?> Filtros(FiltroListadoReporte f) { var d = Fechas(f); d["estado"] = f.Estado; d["cliente"] = f.Cliente; d["tope"] = f.Tope?.ToString(System.Globalization.CultureInfo.InvariantCulture); return d; }
    private static Dictionary<string,string?> Filtros(FiltroCategoriasReporte f) { var d = Fechas(f); d["idCategoria"] = f.IdCategoria?.ToString(System.Globalization.CultureInfo.InvariantCulture); d["top"] = f.Top?.ToString(System.Globalization.CultureInfo.InvariantCulture); return d; }
    public Task<ListadoVentasReporte> Listado(FiltroListadoReporte f, CancellationToken ct) => Consultar<ListadoVentasReporte>("ventas", Filtros(f), ct);
    public Task<CategoriasReporte> Categorias(FiltroCategoriasReporte f, CancellationToken ct) => Consultar<CategoriasReporte>("ventas-por-categoria", Filtros(f), ct);
    public Task<ArchivoReporte> DescargarListado(FiltroListadoReporte f, string formato, CancellationToken ct) => Descargar("ventas", Filtros(f), formato, ct);
    public Task<ArchivoReporte> DescargarCategorias(FiltroCategoriasReporte f, string formato, CancellationToken ct) => Descargar("ventas-por-categoria", Filtros(f), formato, ct);
    public async Task<T> Consultar<T>(string reporte, Dictionary<string,string?> filtros, CancellationToken ct)
    {
        var archivo = await Descargar(reporte, filtros, "json", ct);
        try { return JsonSerializer.Deserialize<T>(archivo.Contenido, new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? throw new ReportesApiException(503); }
        catch (JsonException) { throw new ReportesApiException(503); }
    }
    public async Task<ArchivoReporte> Descargar(string reporte, Dictionary<string,string?> filtros, string formato, CancellationToken ct)
    {
        if (reporte is not ("ventas" or "ventas-por-categoria") || formato is not ("json" or "pdf" or "xlsx")) throw new ReportesApiException(400);
        var token = resolucion.ObtenerToken() ?? throw new ReportesApiException(401);
        var parametros = new Dictionary<string,string?>(filtros) { ["formato"] = formato };
        using var solicitud = new HttpRequestMessage(HttpMethod.Get, QueryHelpers.AddQueryString("api/reportes/" + reporte, parametros));
        solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        try {
            using var respuesta = await http.SendAsync(solicitud, ct);
            if (respuesta.StatusCode == System.Net.HttpStatusCode.Unauthorized) {
                await resolucion.InvalidarAsync();
                throw new ReportesApiException(401);
            }
            if (!respuesta.IsSuccessStatusCode) throw new ReportesApiException((int)respuesta.StatusCode is 400 or 401 or 403 or 500 ? (int)respuesta.StatusCode : 503);
            var tipo = formato switch { "pdf" => "application/pdf", "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", _ => "application/json" };
            if (respuesta.Content.Headers.ContentType?.MediaType != tipo) throw new ReportesApiException(503);
            return new(await respuesta.Content.ReadAsByteArrayAsync(ct), tipo, reporte + "." + formato);
        }
        catch (HttpRequestException) { throw new ReportesApiException(503); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { throw new ReportesApiException(503); }
    }
}

public interface IReportesApiClient
{
    Task<ListadoVentasReporte> Listado(FiltroListadoReporte filtro, CancellationToken ct);
    Task<CategoriasReporte> Categorias(FiltroCategoriasReporte filtro, CancellationToken ct);
    Task<ArchivoReporte> DescargarListado(FiltroListadoReporte filtro, string formato, CancellationToken ct);
    Task<ArchivoReporte> DescargarCategorias(FiltroCategoriasReporte filtro, string formato, CancellationToken ct);
}
