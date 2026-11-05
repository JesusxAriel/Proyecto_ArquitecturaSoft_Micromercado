using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PuntoMarket.Ventas.Frontend.Clients;

namespace PuntoMarket.Ventas.Frontend.Pages.Reportes;

[Authorize(Roles = "Administrador")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class VentasModel(IReportesApiClient api) : ReportePageModel
{
    [BindProperty(SupportsGet = true)] public FiltroListadoReporte Filtro { get; set; } = new();
    public ListadoVentasReporte? Reporte { get; private set; }
    public Task<IActionResult> OnGetAsync(CancellationToken ct) => Ejecutar(async () => Reporte = await api.Listado(Filtro, ct));
    public Task<IActionResult> OnGetPdfAsync(CancellationToken ct) => Descargar(() => api.DescargarListado(Filtro, "pdf", ct));
    public Task<IActionResult> OnGetExcelAsync(CancellationToken ct) => Descargar(() => api.DescargarListado(Filtro, "xlsx", ct));
}
