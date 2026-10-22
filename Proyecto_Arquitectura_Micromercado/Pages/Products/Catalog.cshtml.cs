using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Arquitectura_Micromercado.Application.Common;
using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Application.Security;
using Proyecto_Arquitectura_Micromercado.Application.Stock;
using Proyecto_Arquitectura_Micromercado.Domain.Common;
using Proyecto_Arquitectura_Micromercado.Domain.Products;
using Proyecto_Arquitectura_Micromercado.Domain.Stock;

namespace Proyecto_Arquitectura_Micromercado.Pages.Products;

[Authorize(Roles = Roles.AdministradorYCajero)]
public sealed class CatalogModel(
    IProductService productService,
    IBajaDeLotesService bajaDeLotesService) : PageModel
{
    public PagedResult<ProductListItem> PagedProducts { get; private set; } = new();
    public IReadOnlyList<ProductListItem> Products => PagedProducts.Items;
    public string? DatabaseWarning { get; private set; }

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public int Pagina { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int Tamano { get; set; } = PageSizes.Default;

    [BindProperty(SupportsGet = true)]
    public string? Q { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        try
        {
            PagedProducts = await productService.GetPagedForSalesAsync(
                Pagina, Tamano, Q, cancellationToken);
            Pagina = PagedProducts.Page;
            Tamano = PagedProducts.PageSize;
        }
        catch (ErrorDePersistenciaException)
        {
            DatabaseWarning = "No se pudo cargar el catálogo. Intente nuevamente.";
        }
    }

    public async Task<IActionResult> OnPostDarDeBajaLotesVencidosAsync(CancellationToken cancellationToken)
    {
        if (!User.IsInRole(Roles.Administrador))
        {
            return Forbid();
        }

        try
        {
            var resultado = await bajaDeLotesService.DarDeBajaVencidosAsync(cancellationToken);
            StatusMessage = MensajeDeBaja(resultado);
        }
        catch (ErrorDePersistenciaException)
        {
            ErrorMessage = "No se pudieron dar de baja los lotes vencidos. Intente nuevamente.";
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }

    private static string MensajeDeBaja(ResultadoBajaDeLotes resultado)
    {
        if (resultado.Lotes == 0)
        {
            return "No hay lotes vencidos con stock para dar de baja.";
        }

        var unidades = resultado.Unidades == 1 ? "1 unidad" : $"{resultado.Unidades} unidades";

        return resultado.Lotes == 1
            ? $"Se dio de baja 1 lote vencido con {unidades} en total."
            : $"Se dieron de baja {resultado.Lotes} lotes vencidos con {unidades} en total.";
    }
}
