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
    IClienteCreacionService clienteCreacionService,
    ILogger<IndexModel> logger) : PageModel
{
    private static readonly TimeSpan TiempoMaximoBusqueda = TimeSpan.FromSeconds(8);
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
            venta.Lineas is null ||
            venta.Lineas.Any(linea => linea is null || linea.IdProducto is null || linea.Cantidad is null || linea.PrecioEsperado is null))
        {
            return new JsonResult(new { exito = false, mensaje = "Revise el cliente y las cantidades de la venta. Todos son obligatorios y deben ser válidos." })
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
                        venta.IdCliente!.Value,
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
            new { exito = false, mensaje = "No se pudo registrar la venta. Revise el stock disponible o intente nuevamente." });
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

    public sealed class RegistrarVentaInput
    {
        [Required, Range(1, int.MaxValue)]
        public int? IdCliente { get; init; }

        [Required, MinLength(1)]
        public List<LineaInput>? Lineas { get; init; }
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
