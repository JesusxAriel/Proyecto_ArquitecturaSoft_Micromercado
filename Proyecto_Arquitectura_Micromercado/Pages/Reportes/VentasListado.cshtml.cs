using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Proyecto_Arquitectura_Micromercado.Application.Security;
using Proyecto_Arquitectura_Micromercado.Dtos.Reportes;
using Proyecto_Arquitectura_Micromercado.Servicios.Reportes;

namespace Proyecto_Arquitectura_Micromercado.Pages.Reportes;

[Authorize(Roles = Roles.Administrador)]
public sealed class VentasListadoModel(IReportesApiClient reportes) : PageModel
{
    private const string ClaveDesde = "Filtros.Desde";
    private const string ClaveHasta = "Filtros.Hasta";
    private const string MensajeFechaInvalida = "Ingrese una fecha válida.";
    private const string MensajeRangoInvertido = "La fecha «Desde» no puede ser posterior a la fecha «Hasta».";
    private const string EstadoAnulada = "ANULADA";

    [BindProperty(SupportsGet = true)]
    public FiltrosDeListado Filtros { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public bool Consultar { get; set; }

    public RespuestaListadoVentasDto? Resultado { get; private set; }

    public string? MensajeDeError { get; private set; }

    public IReadOnlyList<SelectListItem> Estados { get; } =
    [
        new("Completada", "COMPLETADA"),
        new("Anulada", EstadoAnulada)
    ];

    public IDictionary<string, string?> ParametrosDeConsulta => new Dictionary<string, string?>
    {
        ["Consultar"] = "true",
        [ClaveDesde] = Filtros.Desde?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        [ClaveHasta] = Filtros.Hasta?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        ["Filtros.Estado"] = Filtros.Estado,
        ["Filtros.Cliente"] = Filtros.Cliente,
        ["Filtros.Tope"] = Filtros.Tope?.ToString(CultureInfo.InvariantCulture)
    };

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        if (Consultar)
            await ConsultarAsync(cancellationToken);
    }

    public Task<IActionResult> OnGetDescargarPdfAsync(CancellationToken cancellationToken) =>
        DescargarAsync(FormatoDeDescarga.Pdf, cancellationToken);

    public Task<IActionResult> OnGetDescargarExcelAsync(CancellationToken cancellationToken) =>
        DescargarAsync(FormatoDeDescarga.Xlsx, cancellationToken);

    public static string NombreDelEstado(string estado) =>
        estado == EstadoAnulada ? "Anulada" : "Completada";

    public static string ClaseDelEstado(string estado) =>
        estado == EstadoAnulada ? "badge bg-warning text-dark" : "badge bg-success";

    private async Task ConsultarAsync(CancellationToken cancellationToken)
    {
        if (!FiltrosSonValidos())
            return;

        try
        {
            Resultado = await reportes.ObtenerListadoDeVentasAsync(Filtros.ADto(), cancellationToken);
        }
        catch (ReporteNoDisponibleException excepcion)
        {
            MensajeDeError = excepcion.Message;
        }
    }

    private async Task<IActionResult> DescargarAsync(
        FormatoDeDescarga formato,
        CancellationToken cancellationToken)
    {
        if (!FiltrosSonValidos())
            return Page();

        try
        {
            var archivo = await reportes.DescargarListadoDeVentasAsync(Filtros.ADto(), formato, cancellationToken);
            return File(archivo.Contenido, archivo.TipoDeContenido, archivo.NombreDeArchivo);
        }
        catch (ReporteNoDisponibleException excepcion)
        {
            MensajeDeError = excepcion.Message;
            return Page();
        }
    }

    private bool FiltrosSonValidos()
    {
        TraducirErroresDeFecha();
        ValidarRango();

        return ModelState.IsValid;
    }

    private void TraducirErroresDeFecha()
    {
        foreach (var clave in new[] { ClaveDesde, ClaveHasta })
        {
            if (ModelState.TryGetValue(clave, out var entrada) && entrada.Errors.Count > 0)
            {
                ModelState.Remove(clave);
                ModelState.AddModelError(clave, MensajeFechaInvalida);
            }
        }
    }

    private void ValidarRango()
    {
        if (Filtros.Desde > Filtros.Hasta)
            ModelState.AddModelError(ClaveDesde, MensajeRangoInvertido);
    }
}
