using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Proyecto_Arquitectura_Micromercado.Application.Categories;
using Proyecto_Arquitectura_Micromercado.Application.Security;
using Proyecto_Arquitectura_Micromercado.Dtos.Reportes;
using Proyecto_Arquitectura_Micromercado.Servicios.Reportes;

namespace Proyecto_Arquitectura_Micromercado.Pages.Reportes;

[Authorize(Roles = Roles.Administrador)]
public sealed class ResumenCategoriasModel(
    IReportesApiClient reportes,
    ICategoryService categorias) : PageModel
{
    private const string ClaveDesde = "Filtros.Desde";
    private const string ClaveHasta = "Filtros.Hasta";
    private const string MensajeFechaInvalida = "Ingrese una fecha válida.";
    private const string MensajeRangoInvertido = "La fecha «Desde» no puede ser posterior a la fecha «Hasta».";
    private const int AnchoMinimoDeBarra = 2;

    [BindProperty(SupportsGet = true)]
    public FiltrosDeResumen Filtros { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public bool Consultar { get; set; }

    public IReadOnlyList<SelectListItem> Categorias { get; private set; } = [];

    public RespuestaVentasPorCategoriaDto? Resultado { get; private set; }

    public string? MensajeDeError { get; private set; }

    public string DescripcionDelGrafico => Resultado is null
        ? string.Empty
        : "Gráfico de barras horizontales del monto vendido por categoría: " +
          string.Join("; ", Resultado.Filas.Select(f => $"{f.Categoria}, Bs {f.MontoVendido:N2}"));

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await CargarCategoriasAsync(cancellationToken);
        if (Consultar)
            await ConsultarAsync(cancellationToken);
    }

    public IDictionary<string, string?> ParametrosDeConsulta => new Dictionary<string, string?>
    {
        ["Consultar"] = "true",
        [ClaveDesde] = Filtros.Desde?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        [ClaveHasta] = Filtros.Hasta?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        ["Filtros.IdCategoria"] = Filtros.IdCategoria?.ToString(CultureInfo.InvariantCulture),
        ["Filtros.Top"] = Filtros.Top?.ToString(CultureInfo.InvariantCulture)
    };

    public Task<IActionResult> OnGetDescargarPdfAsync(CancellationToken cancellationToken) =>
        DescargarAsync(FormatoDeDescarga.Pdf, cancellationToken);

    public Task<IActionResult> OnGetDescargarExcelAsync(CancellationToken cancellationToken) =>
        DescargarAsync(FormatoDeDescarga.Xlsx, cancellationToken);

    public int AnchoDeBarra(decimal monto)
    {
        var maximo = Resultado?.Filas.Max(f => f.MontoVendido) ?? 0m;
        if (maximo <= 0m || monto <= 0m)
            return 0;

        return Math.Max(AnchoMinimoDeBarra, (int)Math.Round(monto / maximo * 100m));
    }

    private async Task ConsultarAsync(CancellationToken cancellationToken)
    {
        if (!FiltrosSonValidos())
            return;

        try
        {
            Resultado = await reportes.ObtenerVentasPorCategoriaAsync(Filtros.ADto(), cancellationToken);
        }
        catch (ReporteNoDisponibleException excepcion)
        {
            MensajeDeError = excepcion.Message;
        }
    }

    private async Task<IActionResult> DescargarAsync(FormatoDeDescarga formato, CancellationToken cancellationToken)
    {
        await CargarCategoriasAsync(cancellationToken);
        if (!FiltrosSonValidos())
        {
            MensajeDeError = PrimerErrorDeFiltros();
            return Page();
        }

        try
        {
            var archivo = await reportes.DescargarVentasPorCategoriaAsync(Filtros.ADto(), formato, cancellationToken);
            return File(archivo.Contenido, archivo.TipoDeContenido, archivo.NombreDeArchivo);
        }
        catch (ReporteNoDisponibleException excepcion)
        {
            MensajeDeError = excepcion.Message;
            return Page();
        }
    }

    private async Task CargarCategoriasAsync(CancellationToken cancellationToken)
    {
        var todas = await categorias.GetAllAsync(cancellationToken);
        Categorias = todas
            .Where(categoria => categoria.IsActive)
            .OrderBy(categoria => categoria.Name)
            .Select(categoria => new SelectListItem(categoria.Name, categoria.Id.ToString()))
            .ToList();
    }

    private string? PrimerErrorDeFiltros() =>
        ModelState.Values
            .SelectMany(entrada => entrada.Errors)
            .Select(error => error.ErrorMessage)
            .FirstOrDefault(mensaje => !string.IsNullOrWhiteSpace(mensaje));

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
