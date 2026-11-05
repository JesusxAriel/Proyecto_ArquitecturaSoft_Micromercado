using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PuntoMarket.Ventas.Frontend.Clients;

namespace PuntoMarket.Ventas.Frontend.Pages.Sales;

[Authorize(Roles = "Cajero,Administrador")]
public sealed class HistorialModel : PageModel
{
    private readonly IVentasApiClient api;
    public HistorialModel(IVentasApiClient api) => this.api = api;
    public bool PuedeAnular => User.IsInRole("Administrador");
    public void OnGet() { }
    public async Task<IActionResult> OnGetHistorialAsync(bool incluirAnuladas, CancellationToken ct)
    {
        try { return new JsonResult(new { ventas = await api.Historial(incluirAnuladas, ct) }); }
        catch (ConsultaApiException e) { return Error(e.Estado, e.Mensaje); }
    }
    public async Task<IActionResult> OnGetDetalleAsync(int idVenta, CancellationToken ct)
    {
        if (idVenta <= 0) return Error(400, "Identificador de venta inválido.");
        try { return new JsonResult(await api.Detalle(idVenta, ct)); }
        catch (ConsultaApiException e) { return Error(e.Estado, e.Mensaje); }
    }
    public async Task<IActionResult> OnPostAnularVentaAsync(
        [FromBody] SolicitudAnulacionFrontend? solicitud,
        CancellationToken ct)
    {
        if (!User.IsInRole("Administrador"))
            return Error(403, "No tiene permiso para anular ventas.");
        if (!ModelState.IsValid || solicitud is null || solicitud.IdVenta <= 0 || string.IsNullOrWhiteSpace(solicitud.Motivo)
            || solicitud.Motivo.Trim().Length > 255)
            return Error(400, "Debe indicar una venta y un motivo válido para la anulación.");

        try
        {
            await api.AnularVenta(solicitud.IdVenta, solicitud.Motivo.Trim(), ct);
            return new JsonResult(new { exito = true });
        }
        catch (ConsultaApiException e) { return Error(e.Estado, e.Mensaje); }
    }
    private static JsonResult Error(int estado, string mensaje) => new(new { mensaje }) { StatusCode = estado };
}
