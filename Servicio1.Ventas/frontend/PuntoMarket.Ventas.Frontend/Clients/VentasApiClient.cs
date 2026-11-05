using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using PuntoMarket.Ventas.Frontend.Security;

namespace PuntoMarket.Ventas.Frontend.Clients;

public sealed record ProductoVenta(int Id, string Nombre, string Presentacion, decimal PrecioVenta, int StockDisponible);
public sealed record ProductosResultado(IReadOnlyList<ProductoVenta> Productos, bool HayMas, int Total, string? Mensaje);
public sealed record ClienteVenta(int Id, string NitCi, string RazonSocial);
public sealed record ClienteCreacionFrontend(string NitCi, string RazonSocial);
public sealed record PrecioProducto(int Id, decimal PrecioVenta);
public sealed record StockProducto(int Id, int StockDisponible);
public sealed record LineaVentaFrontend(int IdProducto, int Cantidad, decimal PrecioEsperado);
public sealed record ResultadoVentaFrontend(int IdVenta, decimal Total);
public sealed record VentaResumenFrontend(int Id, string NroComprobante, DateTime FechaHora, string NombreCliente, string Estado, decimal Total);
public sealed record LoteConsumidoFrontend(string CodigoLote, int Cantidad);
public sealed record LineaDetalleVentaFrontend(int Cantidad, string Descripcion, decimal PrecioUnitario, decimal Subtotal, IReadOnlyList<LoteConsumidoFrontend> Lotes);
public sealed record VentaDetalleFrontend(
    int Id, string NroComprobante, DateTime FechaHora, string NitCiCliente, string RazonSocialCliente,
    string UsuarioCajero, string Estado, decimal Total, string? MotivoAnulacion, DateTime? FechaAnulacion,
    string? UsuarioAnulacion, IReadOnlyList<LineaDetalleVentaFrontend> Lineas);
public sealed record SolicitudAnulacionFrontend(int IdVenta, string Motivo);
public interface IVentasApiClient
{
    Task<ProductosResultado> Productos(string termino, CancellationToken ct);
    Task<IReadOnlyList<ClienteVenta>> Clientes(string termino, CancellationToken ct);
    Task<ClienteVenta> CrearCliente(string nitCi, string razonSocial, CancellationToken ct);
    Task<PrecioProducto> Precio(int id, CancellationToken ct);
    Task<StockProducto> Stock(int id, CancellationToken ct);
    Task<ResultadoVentaFrontend> RegistrarVenta(int idCliente, IReadOnlyList<LineaVentaFrontend> lineas, CancellationToken ct);
    Task<IReadOnlyList<VentaResumenFrontend>> Historial(bool incluirAnuladas, CancellationToken ct);
    Task<VentaDetalleFrontend> Detalle(int idVenta, CancellationToken ct);
    Task<string> ComprobanteHtml(int idVenta, CancellationToken ct);
    Task AnularVenta(int idVenta, string motivo, CancellationToken ct);
}
public sealed class ConsultaApiException(int estado, string? mensaje = null) : Exception(mensaje ?? "La consulta no pudo completarse.")
{
    public int Estado { get; } = estado;
    public string Mensaje { get; } = mensaje ?? MensajePorEstado(estado);
    private static string MensajePorEstado(int estado) => estado switch
    {
        401 => "La sesión no es válida. Inicie sesión nuevamente.",
        403 => "No tiene permiso para esta consulta.", 404 => "El producto no existe o está inactivo.",
        409 => "La información cambió. Vuelva a consultar.", 400 => "Revise los filtros de búsqueda.",
        _ => "Ventas no está disponible. Intente nuevamente más tarde."
    };
}
public sealed class VentasApiClient(HttpClient http, ResolucionSesion resolucion) : IVentasApiClient
{
    public Task<ProductosResultado> Productos(string termino, CancellationToken ct) => Get<ProductosResultado>("api/productos?termino=" + Uri.EscapeDataString(termino), ct);
    public Task<IReadOnlyList<ClienteVenta>> Clientes(string termino, CancellationToken ct) => Get<IReadOnlyList<ClienteVenta>>("api/clientes?termino=" + Uri.EscapeDataString(termino), ct);
    public Task<ClienteVenta> CrearCliente(string nitCi, string razonSocial, CancellationToken ct) =>
        Post<ClienteVenta>("api/clientes", new { nitCi, razonSocial }, ct);
    public Task<PrecioProducto> Precio(int id, CancellationToken ct) => Get<PrecioProducto>($"api/productos/{id}/precio", ct);
    public Task<StockProducto> Stock(int id, CancellationToken ct) => Get<StockProducto>($"api/productos/{id}/stock", ct);
    public Task<ResultadoVentaFrontend> RegistrarVenta(int idCliente, IReadOnlyList<LineaVentaFrontend> lineas, CancellationToken ct) =>
        Post<ResultadoVentaFrontend>("api/ventas", new { idCliente, lineas }, ct);
    public Task<IReadOnlyList<VentaResumenFrontend>> Historial(bool incluirAnuladas, CancellationToken ct) =>
        Get<IReadOnlyList<VentaResumenFrontend>>($"api/ventas?incluirAnuladas={incluirAnuladas}", ct);
    public async Task<VentaDetalleFrontend> Detalle(int idVenta, CancellationToken ct)
    {
        try { return await Get<VentaDetalleFrontend>($"api/ventas/{idVenta}", ct); }
        catch (ConsultaApiException e) when (e.Estado == 404)
        {
            throw new ConsultaApiException(404, "La venta solicitada no existe.");
        }
    }
    public async Task<string> ComprobanteHtml(int idVenta, CancellationToken ct)
    {
        if (idVenta <= 0) throw new ConsultaApiException(400, "Identificador de venta inválido.");
        try { return await GetText($"api/ventas/{idVenta}/comprobante/html", ct); }
        catch (ConsultaApiException e) when (e.Estado == 404)
        {
            throw new ConsultaApiException(404, "El comprobante de la venta solicitada no existe.");
        }
    }
    public Task AnularVenta(int idVenta, string motivo, CancellationToken ct) =>
        PostNoContent($"api/ventas/{idVenta}/anular", new { motivo }, ct);
    private async Task<T> Get<T>(string ruta, CancellationToken ct)
    {
        var token = resolucion.ObtenerToken() ?? throw new ConsultaApiException(401);
        using var solicitud = new HttpRequestMessage(HttpMethod.Get, ruta);
        solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        try
        {
            using var respuesta = await http.SendAsync(solicitud, ct);
            if (respuesta.StatusCode == System.Net.HttpStatusCode.Unauthorized) {
                await resolucion.InvalidarAsync();
                throw new ConsultaApiException(401);
            }
            if (!respuesta.IsSuccessStatusCode)
            {
                var estado = (int)respuesta.StatusCode;
                throw new ConsultaApiException(estado is 400 or 401 or 403 or 404 or 409 ? estado : 503);
            }
            return await respuesta.Content.ReadFromJsonAsync<T>(cancellationToken: ct) ?? throw new ConsultaApiException(503);
        }
        catch (HttpRequestException) { throw new ConsultaApiException(503); }
        catch (JsonException) { throw new ConsultaApiException(503); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { throw new ConsultaApiException(503); }
    }
    private async Task<string> GetText(string ruta, CancellationToken ct)
    {
        var token = resolucion.ObtenerToken() ?? throw new ConsultaApiException(401);
        using var solicitud = new HttpRequestMessage(HttpMethod.Get, ruta);
        solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        try
        {
            using var respuesta = await http.SendAsync(solicitud, ct);
            if (respuesta.StatusCode == System.Net.HttpStatusCode.Unauthorized) {
                await resolucion.InvalidarAsync();
                throw new ConsultaApiException(401);
            }
            if (!respuesta.IsSuccessStatusCode)
            {
                var estado = (int)respuesta.StatusCode;
                throw new ConsultaApiException(estado is 400 or 401 or 403 or 404 or 409 ? estado : 503);
            }
            return await respuesta.Content.ReadAsStringAsync(ct);
        }
        catch (ConsultaApiException) { throw; }
        catch (HttpRequestException) { throw new ConsultaApiException(503); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { throw new ConsultaApiException(503); }
    }
    private async Task<T> Post<T>(string ruta, object cuerpo, CancellationToken ct)
    {
        var token = resolucion.ObtenerToken() ?? throw new ConsultaApiException(401);
        using var solicitud = new HttpRequestMessage(HttpMethod.Post, ruta)
        {
            Content = JsonContent.Create(cuerpo)
        };
        solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        try
        {
            using var respuesta = await http.SendAsync(solicitud, ct);
            if (respuesta.StatusCode == System.Net.HttpStatusCode.Unauthorized) {
                await resolucion.InvalidarAsync();
                throw new ConsultaApiException(401);
            }
            if (!respuesta.IsSuccessStatusCode)
            {
                var estado = (int)respuesta.StatusCode;
                if (estado == 403) throw new ConsultaApiException(403);
                var problema = await respuesta.Content.ReadFromJsonAsync<ProblemaApi>(cancellationToken: ct);
                throw new ConsultaApiException(
                    estado is 400 or 401 or 403 or 404 or 409 ? estado : 503,
                    MensajeDeProblema(estado, problema?.Code));
            }
            return await respuesta.Content.ReadFromJsonAsync<T>(cancellationToken: ct) ?? throw new ConsultaApiException(503);
        }
        catch (ConsultaApiException) { throw; }
        catch (HttpRequestException) { throw new ConsultaApiException(503); }
        catch (JsonException) { throw new ConsultaApiException(503); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { throw new ConsultaApiException(503); }
    }
    private async Task PostNoContent(string ruta, object cuerpo, CancellationToken ct)
    {
        var token = resolucion.ObtenerToken() ?? throw new ConsultaApiException(401);
        using var solicitud = new HttpRequestMessage(HttpMethod.Post, ruta)
        {
            Content = JsonContent.Create(cuerpo)
        };
        solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        try
        {
            using var respuesta = await http.SendAsync(solicitud, ct);
            if (respuesta.StatusCode == System.Net.HttpStatusCode.Unauthorized) {
                await resolucion.InvalidarAsync();
                throw new ConsultaApiException(401);
            }
            if (!respuesta.IsSuccessStatusCode)
            {
                var estado = (int)respuesta.StatusCode;
                if (estado == 403) throw new ConsultaApiException(403);
                var problema = await respuesta.Content.ReadFromJsonAsync<ProblemaApi>(cancellationToken: ct);
                throw new ConsultaApiException(
                    estado is 400 or 401 or 403 or 404 or 409 ? estado : 503,
                    MensajeDeAnulacion(estado, problema?.Code));
            }
        }
        catch (ConsultaApiException) { throw; }
        catch (HttpRequestException) { throw new ConsultaApiException(503); }
        catch (JsonException) { throw new ConsultaApiException(503); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { throw new ConsultaApiException(503); }
    }
    private static string MensajeDeAnulacion(int estado, string? codigo) => codigo switch
    {
        "venta_no_anulable" => "La venta no existe o ya fue anulada.",
        "restauracion_invalida" => "No se puede restaurar el stock de la venta. La operación fue cancelada.",
        "validacion" => "Debe indicar un motivo válido para la anulación.",
        _ => estado switch
        {
            400 => "Revise el motivo de la anulación.",
            401 => "La sesión no es válida. Inicie sesión nuevamente.",
            403 => "No tiene permiso para anular ventas.",
            404 => "La venta no existe.",
            409 => "La venta ya fue anulada o cambió su stock. Actualice el historial.",
            _ => "Ventas no está disponible. Intente nuevamente más tarde."
        }
    };
    private static string MensajeDeProblema(int estado, string? codigo) => codigo switch
    {
        "precio_cambiado" => "El precio vigente cambió. Actualice el producto y vuelva a intentarlo.",
        "stock_insuficiente" => "El stock cambió y ya no alcanza para completar la venta.",
        "cliente_no_disponible" => "El cliente seleccionado ya no está disponible.",
        "producto_no_disponible" => "Uno de los productos seleccionados ya no está disponible.",
        "validacion" => "Revise el cliente, los productos y las cantidades de la venta.",
        "cliente_duplicado" => "Ya existe un cliente activo con ese CI/NIT.",
        _ => estado switch
        {
            401 => "La sesión no es válida. Inicie sesión nuevamente.",
            403 => "No tiene permiso para registrar ventas.",
            409 => "La información cambió. Vuelva a consultar antes de registrar.",
            _ => "Ventas no está disponible. Intente nuevamente más tarde."
        }
    };
    private sealed record ProblemaApi(string? Code);
}
