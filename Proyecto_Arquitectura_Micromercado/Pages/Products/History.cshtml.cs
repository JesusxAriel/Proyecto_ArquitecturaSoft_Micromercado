using Microsoft.AspNetCore.Authorization;
using Proyecto_Arquitectura_Micromercado.Application.Security;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Domain.Products;

namespace Proyecto_Arquitectura_Micromercado.Pages.Products;

[Authorize(Roles = Roles.Administrador)]
public sealed class HistoryModel(IProductService productService) : PageModel
{
    public IReadOnlyList<ProductPriceHistory> PriceHistory { get; private set; } = [];
    public IReadOnlyList<ProductListItem> Products { get; private set; } = [];
    public int? ProductId { get; private set; }
    public string? ProductName { get; private set; }
    public bool IsProductHistory => ProductId.HasValue;

    public async Task<IActionResult> OnGetAsync(int? id, CancellationToken cancellationToken)
    {
        Products = await productService.GetAllAsync(cancellationToken);

        if (!id.HasValue)
        {
            PriceHistory = await productService.GetPriceHistoryAsync(cancellationToken);
            return Page();
        }

        if (id.Value <= 0)
        {
            return BadRequest("El identificador del producto debe ser positivo.");
        }

        var product = await productService.GetByIdAsync(id.Value, cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        ProductId = product.Id;
        ProductName = product.Nombre;
        PriceHistory = await productService.GetPriceHistoryByProductIdAsync(product.Id, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnGetDetalleAsync(int id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return BadRequest();
        }

        var history = await productService.GetPriceHistoryByIdAsync(id, cancellationToken);
        if (history is null)
        {
            return NotFound();
        }

        return Partial("_PriceHistoryDetail", history);
    }
}
