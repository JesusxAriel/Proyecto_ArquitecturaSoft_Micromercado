using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Arquitectura_Micromercado.Application.Common;
using Proyecto_Arquitectura_Micromercado.Application.Customers;
using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Application.Security;

namespace Proyecto_Arquitectura_Micromercado.Pages.Sales;

[Authorize(Roles = Roles.AdministradorYCajero)]
public sealed class IndexModel(
    IClienteConsultaService clienteConsultaService,
    IProductService productService) : PageModel
{
    public SaleInputModel Input { get; } = new();

    public void OnGet()
    {
        // Las búsquedas se realizan bajo demanda desde la pantalla.
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

    public async Task<IActionResult> OnGetBuscarProductosAsync(
        string? termino, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        if (string.IsNullOrWhiteSpace(termino))
        {
            return new JsonResult(new { productos = Array.Empty<object>(), total = 0 });
        }

        try
        {
            var listado = await productService.GetPagedAsync(1, 10, termino, cancellationToken);
            return new JsonResult(new
            {
                productos = listado.Items.Select(producto => new
                {
                    id = producto.Id,
                    nombre = producto.Nombre,
                    presentacion = producto.EmpaquePresentacion,
                    precioVenta = producto.PrecioVenta,
                    stockDisponible = producto.StockCalculado
                }),
                total = listado.TotalCount
            });
        }
        catch (ErrorDePersistenciaException)
        {
            return new JsonResult(new { mensaje = "No se pudo buscar productos. Intente nuevamente." })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable
            };
        }
    }
}
