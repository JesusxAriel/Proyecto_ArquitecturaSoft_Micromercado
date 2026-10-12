using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Arquitectura_Micromercado.Application.Comprobantes;
using Proyecto_Arquitectura_Micromercado.Application.Common;
using Proyecto_Arquitectura_Micromercado.Application.Security;

namespace Proyecto_Arquitectura_Micromercado.Pages.Comprobante;

[Authorize(Roles = Roles.AdministradorYCajero)]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class IndexModel(
    IComprobanteService comprobantes,
    IGeneradorComprobante generador) : PageModel
{
    public string? MensajeDeError { get; private set; }

    public async Task<IActionResult> OnGetAsync(int idVenta, CancellationToken cancellationToken)
    {
        try
        {
            var comprobante = await comprobantes.ObtenerAsync(idVenta, cancellationToken);

            if (comprobante is null)
            {
                Response.StatusCode = StatusCodes.Status404NotFound;
                MensajeDeError = "No se encontró ninguna venta con ese número.";
                return Page();
            }

            return Content(generador.GenerarHtml(comprobante), "text/html; charset=utf-8");
        }
        catch (ErrorDePersistenciaException)
        {
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            MensajeDeError = "No se pudo conectar con la base de datos. Intente nuevamente.";
            return Page();
        }
    }
}
