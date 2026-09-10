using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Arquitectura_Micromercado.Application.Suppliers;
using Proyecto_Arquitectura_Micromercado.Domain.Suppliers;

namespace Proyecto_Arquitectura_Micromercado.Pages.Suppliers;

public sealed class IndexModel(ISupplierService supplierService) : PageModel
{
    public IReadOnlyList<SupplierListItem> Suppliers { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Suppliers = await supplierService.GetAllAsync(cancellationToken);
    }
}