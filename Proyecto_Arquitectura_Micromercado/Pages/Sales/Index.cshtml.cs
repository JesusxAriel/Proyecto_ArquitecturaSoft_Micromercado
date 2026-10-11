using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Arquitectura_Micromercado.Application.Customers;
using Proyecto_Arquitectura_Micromercado.Application.Security;

namespace Proyecto_Arquitectura_Micromercado.Pages.Sales;

[Authorize(Roles = Roles.AdministradorYCajero)]
public sealed class IndexModel(IClienteConsultaService clienteConsultaService) : PageModel
{
    public SaleInputModel Input { get; } = new();

    public void OnGet()
    {
        // La búsqueda de clientes se realiza bajo demanda desde la pantalla.
    }

    public async Task<JsonResult> OnGetBuscarClientesAsync(
        string? termino, CancellationToken cancellationToken)
    {
        var clientes = await clienteConsultaService.BuscarAsync(termino, cancellationToken);
        return new JsonResult(clientes.Select(cliente => new
        {
            id = cliente.Id,
            nitCi = cliente.NitCi,
            razonSocial = cliente.RazonSocial
        }));
    }
}
