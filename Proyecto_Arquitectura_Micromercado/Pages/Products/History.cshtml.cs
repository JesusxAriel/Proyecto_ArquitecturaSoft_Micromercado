using Microsoft.AspNetCore.Authorization;
using Proyecto_Arquitectura_Micromercado.Application.Security;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Domain.Products;

namespace Proyecto_Arquitectura_Micromercado.Pages.Products;

[Authorize(Roles = Roles.Administrador)]
public sealed class HistoryModel(IProductService productService) : PageModel
{
    public IReadOnlyList<ProductPriceHistory> PriceHistory { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        PriceHistory = await productService.GetPriceHistoryAsync(cancellationToken);
    }
}
