using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PuntoMarket.Ventas.Frontend.Clients;

namespace PuntoMarket.Ventas.Frontend.Pages.Reportes;

[Authorize(Roles = "Administrador")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class CategoriasModel(IReportesApiClient api) : ReportePageModel
{
    [BindProperty(SupportsGet = true)] public FiltroCategoriasReporte Filtro { get; set; } = new();
    public CategoriasReporte? Reporte { get; private set; }
    public IReadOnlyList<CategoriaReporte> Categorias { get; private set; } = [];
    private async Task CargarCategorias(CancellationToken ct)
    {
        try { Categorias = await api.ListarCategorias(ct); }
        catch (ReportesApiException) { Categorias = []; }
    }
    private const int AnchoMinimoDeBarra = 2;
    public int AnchoDeBarra(decimal monto)
    {
        var maximo = Reporte is { Filas.Count: > 0 } ? Reporte.Filas.Max(f => f.MontoVendido) : 0m;
        if (maximo <= 0m || monto <= 0m) return 0;
        return Math.Max(AnchoMinimoDeBarra, (int)Math.Round(monto / maximo * 100m));
    }
    public string DescripcionDelGrafico => Reporte is null ? "" :
        "Monto vendido por categoría: " + string.Join("; ", Reporte.Filas.Select(f => $"{f.Categoria}, Bs {f.MontoVendido:N2}"));
    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        await CargarCategorias(ct);
        return await Ejecutar(async () => Reporte = await api.Categorias(Filtro, ct));
    }
    public Task<IActionResult> OnGetPdfAsync(CancellationToken ct) => Descargar(() => api.DescargarCategorias(Filtro, "pdf", ct));
    public Task<IActionResult> OnGetExcelAsync(CancellationToken ct) => Descargar(() => api.DescargarCategorias(Filtro, "xlsx", ct));
}
