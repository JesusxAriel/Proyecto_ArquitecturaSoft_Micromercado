using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Arquitectura_Micromercado.Application.Common;
using Proyecto_Arquitectura_Micromercado.Application.Customers;
using Proyecto_Arquitectura_Micromercado.Application.Facades;
using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Application.Security;
using Proyecto_Arquitectura_Micromercado.Application.Sales;

namespace Proyecto_Arquitectura_Micromercado.Pages.Sales;

[Authorize(Roles = Roles.AdministradorYCajero)]
public sealed class IndexModel(
    IClienteConsultaService clienteConsultaService,
    IProductService productService,
    IFachadaVenta fachadaVenta,
    IVentaConsultaService ventaConsultaService,
    IFachadaAnulacion fachadaAnulacion,
    IClienteCreacionService clienteCreacionService,
    ILogger<IndexModel> logger) : PageModel
{
    public SaleInputModel Input { get; } = new();
    public DateTime FechaActual { get; } = DateTime.Now;

    public void OnGet()
    {
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
            var resultado = await fachadaVenta.RegistrarVentaAsync(new NuevaVentaDto(
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

    public async Task<JsonResult> OnPostCrearClienteAsync(
        [FromBody] ClienteCreacionDto? cliente, CancellationToken cancellationToken)
    {
        if (cliente is null || !ModelState.IsValid)
        {
            var mensajes = ModelState.Values
                .SelectMany(estado => estado.Errors)
                .Select(error => error.ErrorMessage)
                .Where(mensaje => !string.IsNullOrWhiteSpace(mensaje))
                .Distinct();
            var texto = string.Join(" ", mensajes);

            return new JsonResult(new
            {
                exito = false,
                mensaje = string.IsNullOrWhiteSpace(texto) ? "Ingrese el CI/NIT y la razón social." : texto
            })
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
        }

        try
        {
            var id = await clienteCreacionService.CrearAsync(cliente, cancellationToken);
            return new JsonResult(new
            {
                exito = true,
                id,
                nitCi = cliente.NitCi.Trim(),
                razonSocial = cliente.RazonSocial.Trim()
            });
        }
        catch (Exception ex) when (ex is ClienteDuplicadoException or ValidationException)
        {
            return new JsonResult(new { exito = false, mensaje = ex.Message })
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo registrar el cliente desde ventas.");
            return new JsonResult(new { exito = false, mensaje = "No se pudo registrar el cliente. Intente nuevamente." })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable
            };
        }
    }

    public async Task<JsonResult> OnPostAnularVentaAsync(
        [FromBody] AnularVentaInput? entrada, CancellationToken cancellationToken)
    {
        if (!User.IsInRole(Roles.Administrador))
        {
            return new JsonResult(new { exito = false, mensaje = "Solo el administrador puede anular ventas." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

        if (entrada is null || !ModelState.IsValid || entrada.IdVenta is null || entrada.Motivo is null)
        {
            return new JsonResult(new { exito = false, mensaje = "Indique la venta y el motivo de la anulación (máximo 255 caracteres)." })
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
        }

        try
        {
            await fachadaAnulacion.AnularVentaAsync(entrada.IdVenta.Value, entrada.Motivo, cancellationToken);
            return new JsonResult(new { exito = true });
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
            logger.LogError(ex, "No se pudo anular la venta.");
            return new JsonResult(new { exito = false, mensaje = "No se pudo anular la venta. Intente nuevamente." })
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

    public sealed class AnularVentaInput
    {
        [Required, Range(1, int.MaxValue)]
        public int? IdVenta { get; init; }

        [Required, StringLength(255)]
        public string? Motivo { get; init; }
    }

    public sealed class LineaInput
    {
        [Required, Range(1, int.MaxValue)]
        public int? IdProducto { get; init; }

        [Required, Range(1, int.MaxValue)]
        public int? Cantidad { get; init; }
    }

    public async Task<IActionResult> OnGetHistorialAsync(
        bool incluirAnuladas, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";

        try
        {
            var ventas = await ventaConsultaService.ListarAsync(incluirAnuladas, cancellationToken);
            return new JsonResult(new
            {
                ventas = ventas.Select(venta => new
                {
                    id = venta.Id,
                    nroComprobante = venta.NroComprobante,
                    fechaHora = venta.FechaHora,
                    nombreCliente = venta.NombreCliente,
                    estado = venta.Estado,
                    total = venta.Total
                })
            });
        }
        catch (ErrorDePersistenciaException)
        {
            return new JsonResult(new { mensaje = "No se pudo cargar el historial de ventas. Intente nuevamente." })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable
            };
        }
    }

    public async Task<IActionResult> OnGetDetalleAsync(
        int idVenta, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";

        try
        {
            var venta = await ventaConsultaService.ObtenerDetalleAsync(idVenta, cancellationToken);
            if (venta is null)
            {
                return new JsonResult(new { mensaje = "La venta solicitada no existe." })
                {
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            return new JsonResult(new
            {
                id = venta.Id,
                nroComprobante = venta.NroComprobante,
                fechaHora = venta.FechaHora,
                nitCiCliente = venta.NitCiCliente,
                razonSocialCliente = venta.RazonSocialCliente,
                usuarioCajero = venta.UsuarioCajero,
                estado = venta.Estado,
                total = venta.Total,
                efectivoPagado = venta.EfectivoPagado,
                cambio = venta.Cambio,
                motivoAnulacion = venta.MotivoAnulacion,
                fechaAnulacion = venta.FechaAnulacion,
                usuarioAnulacion = venta.UsuarioAnulacion,
                lineas = venta.Lineas.Select(linea => new
                {
                    cantidad = linea.Cantidad,
                    descripcion = linea.Descripcion,
                    precioUnitario = linea.PrecioUnitario,
                    subtotal = linea.Subtotal,
                    lotes = linea.Lotes.Select(lote => new
                    {
                        codigoLote = lote.CodigoLote,
                        cantidad = lote.Cantidad
                    })
                })
            });
        }
        catch (ErrorDePersistenciaException)
        {
            return new JsonResult(new { mensaje = "No se pudo cargar el detalle de la venta. Intente nuevamente." })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable
            };
        }
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
