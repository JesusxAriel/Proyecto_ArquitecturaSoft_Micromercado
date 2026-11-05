using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PuntoMarket.Ventas.Frontend.Clients;

namespace PuntoMarket.Ventas.Frontend.Pages.Reportes;

public abstract class ReportePageModel : PageModel
{
    public string? ErrorReporte { get; private set; }
    protected async Task<IActionResult> Ejecutar(Func<Task> consultar)
    {
        if (!ModelState.IsValid) { Response.StatusCode = 400; return Page(); }
        try { await consultar(); }
        catch (ReportesApiException error) { ErrorReporte = error.Message; Response.StatusCode = error.Estado; }
        return Page();
    }
    protected async Task<IActionResult> Descargar(Func<Task<ArchivoReporte>> consultar)
    {
        if (!ModelState.IsValid) { Response.StatusCode = 400; return Page(); }
        try
        {
            var archivo = await consultar();
            return File(archivo.Contenido, archivo.TipoContenido, archivo.Nombre);
        }
        catch (ReportesApiException error) { ErrorReporte = error.Message; Response.StatusCode = error.Estado; return Page(); }
    }
}
