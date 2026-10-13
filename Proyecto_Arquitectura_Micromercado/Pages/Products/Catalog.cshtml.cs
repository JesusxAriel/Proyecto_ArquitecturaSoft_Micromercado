using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Arquitectura_Micromercado.Application.Common;
using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Application.Security;
using Proyecto_Arquitectura_Micromercado.Domain.Common;
using Proyecto_Arquitectura_Micromercado.Domain.Products;

namespace Proyecto_Arquitectura_Micromercado.Pages.Products;

[Authorize(Roles = Roles.AdministradorYCajero)]
public sealed class CatalogModel(IProductService productService) : PageModel
{
    public PagedResult<ProductListItem> PagedProducts { get; private set; } = new();
    public IReadOnlyList<ProductListItem> Products => PagedProducts.Items;
    public string? DatabaseWarning { get; private set; }

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
}