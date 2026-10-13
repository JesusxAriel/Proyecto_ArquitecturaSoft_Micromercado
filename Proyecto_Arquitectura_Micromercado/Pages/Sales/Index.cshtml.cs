using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Arquitectura_Micromercado.Application.Common;
using Proyecto_Arquitectura_Micromercado.Application.Customers;
using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Application.Security;
using Proyecto_Arquitectura_Micromercado.Application.Sales;

namespace Proyecto_Arquitectura_Micromercado.Pages.Sales;

[Authorize(Roles = Roles.AdministradorYCajero)]
public sealed class IndexModel(
    IClienteConsultaService clienteConsultaService,
    IProductService productService,
    IVentaFacade ventaFacade,
    ILogger<IndexModel> logger) : PageModel
{
    public SaleInputModel Input { get; } = new();
    public DateTime FechaActual { get; } = DateTime.Now;

    public void OnGet()
    {
        // Las búsquedas se realizan bajo demanda desde la pantalla.
    }

    public async Task<JsonResult> OnPostRegistrarVentaAsync(
        [FromBody] RegistrarVentaInput? venta, CancellationToken cancellationToken)
    {
        if (venta is null || !ModelState.IsValid || venta.IdCliente is null ||
            venta.EfectivoPagado is null || venta.Lineas is null ||
            venta.Lineas.Any(linea => linea is null || linea.IdProducto is null || linea.Cantidad is null))
        {
            return new JsonResult(new { exito = false, mensaje = "Revise el cliente, el efectivo y las cantidades de la venta. Todos son obligatorios y deben ser válidos." })
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
        }

        try
        {
            var resultado = await ventaFacade.RegistrarVentaAsync(new NuevaVentaDto(
                venta.IdCliente!.Value, venta.EfectivoPagado!.Value,
                venta.Lineas!.Select(linea => new LineaVentaDto(linea.IdProducto!.Value, linea.Cantidad!.Value)).ToArray()),
                cancellationToken);
            return new JsonResult(new { exito = true, idVenta = resultado.IdVenta, total = resultado.Total });
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or ValidationException)
        {
            return new JsonResult(new { exito = false, mensaje = ex.Message })
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo registrar la venta.");
            return new JsonResult(new { exito = false, mensaje = "No se pudo registrar la venta. Revise el stock disponible y el efectivo pagado, o intente nuevamente." })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable
            };
        }
    }

    public sealed class RegistrarVentaInput
    {
        [Required, Range(1, int.MaxValue)]
        public int? IdCliente { get; init; }

        [Required, Range(typeof(decimal), "0", "79228162514264337593543950335")]
        public decimal? EfectivoPagado { get; init; }

        [Required, MinLength(1)]
        public List<LineaInput>? Lineas { get; init; }
    }

    public sealed class LineaInput
    {
        [Required, Range(1, int.MaxValue)]
        public int? IdProducto { get; init; }

        [Required, Range(1, int.MaxValue)]
        public int? Cantidad { get; init; }
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
            var listado = await productService.GetPagedForSalesAsync(1, 10, termino, cancellationToken);
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
