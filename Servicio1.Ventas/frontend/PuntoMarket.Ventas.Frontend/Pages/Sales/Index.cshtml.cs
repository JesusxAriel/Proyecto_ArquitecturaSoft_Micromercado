using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PuntoMarket.Ventas.Frontend.Clients;
using System.ComponentModel.DataAnnotations;

namespace PuntoMarket.Ventas.Frontend.Pages.Sales;

[Authorize(Roles = "Cajero,Administrador")]
[ValidateAntiForgeryToken]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class IndexModel(IVentasApiClient api) : PageModel
{
    public SaleInputModel Input { get; } = new();
    public DateTime FechaActual { get; } = DateTime.Now;
    public void OnGet() { }
    public Task<IActionResult> OnGetBuscarProductosAsync(string? termino, CancellationToken ct) => Consulta(termino, t => api.Productos(t, ct));
    public Task<IActionResult> OnGetBuscarClientesAsync(string? termino, CancellationToken ct) => Consulta(termino, t => api.Clientes(t, ct));
    public Task<IActionResult> OnGetPrecioAsync(int id, CancellationToken ct) => id > 0 ? Ejecutar(() => api.Precio(id, ct)) : Task.FromResult<IActionResult>(Error(400, "Identificador inválido."));
    public Task<IActionResult> OnGetStockAsync(int id, CancellationToken ct) => id > 0 ? Ejecutar(() => api.Stock(id, ct)) : Task.FromResult<IActionResult>(Error(400, "Identificador inválido."));
    public async Task<IActionResult> OnPostRegistrarVentaAsync([FromBody] VentaRegistroFrontend? solicitud, CancellationToken ct)
    {
        if (!ModelState.IsValid || solicitud is null || solicitud.IdCliente <= 0 || solicitud.Lineas is not { Count: > 0 } ||
            solicitud.Lineas.Any(linea => linea is null || linea.IdProducto <= 0 || linea.Cantidad <= 0 || linea.PrecioEsperado < 0))
            return Error(400, "Revise el cliente, los productos y las cantidades de la venta.");
        try
        {
            return new JsonResult(await api.RegistrarVenta(
                solicitud.IdCliente,
                solicitud.Lineas.Select(linea => new LineaVentaFrontend(linea!.IdProducto, linea.Cantidad, linea.PrecioEsperado)).ToArray(),
                ct));
        }
        catch (ConsultaApiException e) { return Error(e.Estado, e.Mensaje); }
    }
    public async Task<IActionResult> OnPostCrearClienteAsync(
        [FromBody] ClienteCreacionFrontend? solicitud, CancellationToken ct)
    {
        if (solicitud is null || string.IsNullOrWhiteSpace(solicitud.NitCi) ||
            solicitud.NitCi.Trim().Length > 30 || string.IsNullOrWhiteSpace(solicitud.RazonSocial) ||
            solicitud.RazonSocial.Trim().Length > 150)
            return Error(400, "Ingrese un CI/NIT y una razón social válidos.");
        try
        {
            return new JsonResult(await api.CrearCliente(solicitud.NitCi.Trim(), solicitud.RazonSocial.Trim(), ct));
        }
        catch (ConsultaApiException e) { return Error(e.Estado, e.Mensaje); }
    }
    private Task<IActionResult> Consulta<T>(string? termino, Func<string, Task<T>> consultar) =>
        termino?.Trim() is { Length: > 0 and <= 150 } normalizado ? Ejecutar(() => consultar(normalizado)) : Task.FromResult<IActionResult>(Error(400, "Introduzca un filtro de hasta 150 caracteres."));
    private async Task<IActionResult> Ejecutar<T>(Func<Task<T>> consultar)
    {
        try { return new JsonResult(await consultar()); }
        catch (ConsultaApiException e) { return Error(e.Estado, e.Mensaje); }
    }
    private static JsonResult Error(int estado, string mensaje) => new(new { mensaje }) { StatusCode = estado };
}

public sealed class VentaRegistroFrontend
{
    [Range(1, int.MaxValue)] public int IdCliente { get; init; }
    [Required, MinLength(1)] public List<LineaRegistroFrontend?>? Lineas { get; init; }
}

public sealed class LineaRegistroFrontend
{
    [Range(1, int.MaxValue)] public int IdProducto { get; init; }
    [Range(1, int.MaxValue)] public int Cantidad { get; init; }
    [Range(0, double.MaxValue)] public decimal PrecioEsperado { get; init; }
}

public sealed class ClienteCreacionFrontend
{
    public string? NitCi { get; init; }
    public string? RazonSocial { get; init; }
}
