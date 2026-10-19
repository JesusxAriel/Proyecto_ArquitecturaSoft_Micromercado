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
using Proyecto_Arquitectura_Micromercado.Pages.Shared;

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
    private static readonly TimeSpan TiempoMaximoBusqueda = TimeSpan.FromSeconds(8);
    private static readonly TimeSpan TiempoMaximoConsulta = TimeSpan.FromSeconds(8);
    private static readonly TimeSpan TiempoMaximoEscritura = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan TiempoMaximoCreacionDeCliente = TimeSpan.FromSeconds(15);

    private readonly EjecutorDePeticiones ejecutor = new(logger);

    public SaleInputModel Input { get; } = new();
    public DateTime FechaActual { get; } = DateTime.Now;

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostRegistrarVentaAsync(
        [FromBody] RegistrarVentaInput? venta, CancellationToken cancellationToken)
    {
        if (venta is null || !ModelState.IsValid || venta.IdCliente is null ||
            venta.EfectivoPagado is null || venta.Lineas is null ||
            venta.Lineas.Any(linea => linea is null || linea.IdProducto is null || linea.Cantidad is null || linea.PrecioEsperado is null))
        {
            return new JsonResult(new { exito = false, mensaje = "Revise el cliente, el efectivo y las cantidades de la venta. Todos son obligatorios y deben ser válidos." })
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
        }

        return await ejecutor.EjecutarAsync(
            cancellationToken,
            TiempoMaximoEscritura,
            async limite =>
            {
                try
                {
                    var resultado = await fachadaVenta.RegistrarVentaAsync(new NuevaVentaDto(
                        venta.IdCliente!.Value, venta.EfectivoPagado!.Value,
                        venta.Lineas!.Select(linea => new LineaVentaDto(linea.IdProducto!.Value, linea.Cantidad!.Value, linea.PrecioEsperado!.Value)).ToArray()),
                        limite);
                    return new JsonResult(new { exito = true, idVenta = resultado.IdVenta, total = resultado.Total });
                }
                catch (PrecioCambiadoException ex)
                {
                    return new JsonResult(new
                    {
                        exito = false,
                        mensaje = ex.Message,
                        codigo = "precio-cambiado",
                        idProducto = ex.IdProducto,
                        precioActual = ex.PrecioVigente
                    })
                    {
                        StatusCode = StatusCodes.Status400BadRequest
                    };
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or ValidationException)
                {
                    return new JsonResult(new { exito = false, mensaje = ex.Message })
                    {
                        StatusCode = StatusCodes.Status400BadRequest
                    };
                }
            },
            "el registro de la venta",
            new { exito = false, mensaje = "No se pudo registrar la venta. Revise el stock disponible y el efectivo pagado, o intente nuevamente." });
    }

    public async Task<IActionResult> OnPostCrearClienteAsync(
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

        return await ejecutor.EjecutarAsync(
            cancellationToken,
            TiempoMaximoCreacionDeCliente,
            async limite =>
            {
                try
                {
                    var id = await clienteCreacionService.CrearAsync(cliente, limite);
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
            },
            "el registro del cliente",
            new { exito = false, mensaje = "No se pudo registrar el cliente. Intente nuevamente." });
    }

    public async Task<IActionResult> OnPostAnularVentaAsync(
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

        return await ejecutor.EjecutarAsync(
            cancellationToken,
            TiempoMaximoEscritura,
            async limite =>
            {
                try
                {
                    await fachadaAnulacion.AnularVentaAsync(entrada.IdVenta.Value, entrada.Motivo, limite);
                    return new JsonResult(new { exito = true });
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or ValidationException)
                {
                    return new JsonResult(new { exito = false, mensaje = ex.Message })
                    {
                        StatusCode = StatusCodes.Status400BadRequest
                    };
                }
            },
            "la anulación de la venta",
            new { exito = false, mensaje = "No se pudo anular la venta. Intente nuevamente." });
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

        [Required, Range(typeof(decimal), "0", "79228162514264337593543950335")]
        public decimal? PrecioEsperado { get; init; }
    }

    public Task<IActionResult> OnGetHistorialAsync(
        bool incluirAnuladas, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";

        return ejecutor.EjecutarAsync(
            cancellationToken,
            TiempoMaximoConsulta,
            async limite =>
            {
                var ventas = await ventaConsultaService.ListarAsync(incluirAnuladas, limite);
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
            },
            "el historial de ventas",
            new { mensaje = "No se pudo cargar el historial de ventas. Intente nuevamente." });
    }

    public Task<IActionResult> OnGetDetalleAsync(
        int idVenta, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";

        return ejecutor.EjecutarAsync(
            cancellationToken,
            TiempoMaximoConsulta,
            async limite =>
            {
                var venta = await ventaConsultaService.ObtenerDetalleAsync(idVenta, limite);
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
            },
            "el detalle de la venta",
            new { mensaje = "No se pudo cargar el detalle de la venta. Intente nuevamente." });
    }

    public async Task<IActionResult> OnGetBuscarClientesAsync(
        string? termino, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        if (cancellationToken.IsCancellationRequested)
        {
            return new EmptyResult();
        }

        using var limite = new CancellationTokenSource(TiempoMaximoBusqueda);
        try
        {
            var clientes = await clienteConsultaService.BuscarAsync(termino, limite.Token);
            if (cancellationToken.IsCancellationRequested)
            {
                return new EmptyResult();
            }

            return new JsonResult(clientes.Select(cliente => new
            {
                id = cliente.Id,
                nitCi = cliente.NitCi,
                razonSocial = cliente.RazonSocial
            }));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new EmptyResult();
        }
        catch (Exception ex)
        {
            return FalloDeBusqueda(
                ex,
                "clientes",
                new { mensaje = "No se pudo buscar clientes. Intente nuevamente." });
        }
    }

    public async Task<IActionResult> OnGetBuscarProductosAsync(
        string? termino, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        if (cancellationToken.IsCancellationRequested)
        {
            return new EmptyResult();
        }

        using var limite = new CancellationTokenSource(TiempoMaximoBusqueda);
        try
        {
            var resultado = await productService.BuscarParaVentaAsync(termino, limite.Token);
            if (cancellationToken.IsCancellationRequested)
            {
                return new EmptyResult();
            }

            return new JsonResult(RespuestaDeProductos(
                resultado.Productos.Select(producto => new
                {
                    id = producto.Id,
                    nombre = producto.Nombre,
                    presentacion = producto.Presentacion,
                    precioVenta = producto.PrecioVenta,
                    stockDisponible = producto.StockDisponible
                }).ToArray(),
                resultado.HayMas,
                null));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new EmptyResult();
        }
        catch (Exception ex)
        {
            return FalloDeBusqueda(
                ex,
                "productos",
                RespuestaDeProductos([], false, "No se pudo buscar productos. Intente nuevamente."));
        }
    }

    private static object RespuestaDeProductos(IReadOnlyCollection<object> productos, bool hayMas, string? mensaje) =>
        new { productos, total = productos.Count, hayMas, mensaje };

    private JsonResult FalloDeBusqueda(Exception ex, string descripcion, object cuerpo)
    {
        if (ex is OperationCanceledException)
        {
            logger.LogWarning("La búsqueda de {Descripcion} superó el tiempo máximo.", descripcion);
        }
        else if (ex is ErrorDePersistenciaException)
        {
            logger.LogWarning(ex, "No se pudo buscar {Descripcion}.", descripcion);
        }
        else
        {
            logger.LogError(ex, "Error inesperado al buscar {Descripcion}.", descripcion);
        }

        return new JsonResult(cuerpo)
        {
            StatusCode = StatusCodes.Status503ServiceUnavailable
        };
    }
}
