using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Arquitectura_Micromercado.Application.Facades;
using Proyecto_Arquitectura_Micromercado.Application.Sales;
using Proyecto_Arquitectura_Micromercado.Application.Security;
using Proyecto_Arquitectura_Micromercado.Pages.Shared;

namespace Proyecto_Arquitectura_Micromercado.Pages.Sales;

[Authorize(Roles = Roles.AdministradorYCajero)]
public sealed class HistorialModel(
    IVentaConsultaService ventaConsultaService,
    IFachadaAnulacion fachadaAnulacion,
    ILogger<HistorialModel> logger) : PageModel
{
    private static readonly TimeSpan TiempoMaximoConsulta = TimeSpan.FromSeconds(8);
    private static readonly TimeSpan TiempoMaximoEscritura = TimeSpan.FromSeconds(30);

    private readonly EjecutorDePeticiones ejecutor = new(logger);

    public bool PuedeAnular => User.IsInRole(Roles.Administrador);

    public void OnGet()
    {
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

    public sealed class AnularVentaInput
    {
        [Required, Range(1, int.MaxValue)]
        public int? IdVenta { get; init; }

        [Required, StringLength(255)]
        public string? Motivo { get; init; }
    }
}
