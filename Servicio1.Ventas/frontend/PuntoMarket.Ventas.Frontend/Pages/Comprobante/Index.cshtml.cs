using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PuntoMarket.Ventas.Frontend.Clients;

namespace PuntoMarket.Ventas.Frontend.Pages.Comprobante;

[Authorize(Roles = "Cajero,Administrador")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class IndexModel(IVentasApiClient api) : PageModel
{
    public async Task<IActionResult> OnGetAsync(int idVenta, CancellationToken cancellationToken)
    {
        if (idVenta <= 0)
        {
            return Error(400, "Identificador de venta inválido.");
        }

        try
        {
            var html = await api.ComprobanteHtml(idVenta, cancellationToken);
            return Content(html, "text/html; charset=utf-8");
        }
        catch (ConsultaApiException e)
        {
            return Error(e.Estado, e.Mensaje);
        }
    }

    private ContentResult Error(int statusCode, string message)
    {
        Response.StatusCode = statusCode;
        return Content($"""
            <!DOCTYPE html>
            <html lang="es">
            <head><meta charset="utf-8" /><title>Comprobante no disponible - PuntoMarket</title></head>
            <body>
            <main>
            <h1>Comprobante no disponible</h1>
            <p>{System.Net.WebUtility.HtmlEncode(message)}</p>
            <a href="/Sales/Historial">Volver al historial</a>
            </main>
            </body>
            </html>
            """, "text/html; charset=utf-8");
    }
}
