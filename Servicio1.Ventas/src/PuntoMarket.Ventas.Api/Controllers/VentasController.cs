using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PuntoMarket.Ventas.Api.Contracts;
using PuntoMarket.Ventas.Api.Security;
using PuntoMarket.Ventas.Application.Facades;
using PuntoMarket.Ventas.Application.Sales;

namespace PuntoMarket.Ventas.Api.Controllers;

[ApiController]
[Route("api/ventas")]
[Authorize(Policy = SeguridadJwt.OperarVentas)]
public sealed class VentasController(
    IFachadaVenta registro, IFachadaAnulacion anulacion, IVentaConsultaService consultas) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Registrar(RegistrarVentaSolicitud solicitud, CancellationToken cancellationToken)
    {
        var dto = new NuevaVentaDto(solicitud.IdCliente, solicitud.Lineas!
            .Select(linea => new LineaVentaDto(linea!.IdProducto, linea.Cantidad, linea.PrecioEsperado)).ToArray());
        var resultado = await registro.RegistrarVentaAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(Detalle), new { id = resultado.IdVenta }, resultado);
    }

    [HttpPost("{id:int}/anular")]
    [Authorize(Policy = SeguridadJwt.AnularVentas)]
    public async Task<IActionResult> Anular([FromRoute, System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)] int id,
        AnularVentaSolicitud solicitud, CancellationToken cancellationToken)
    {
        await anulacion.AnularVentaAsync(id, solicitud.Motivo!, cancellationToken);
        return NoContent();
    }

    [HttpGet]
    public async Task<IActionResult> Historial([FromQuery] bool incluirAnuladas = false, CancellationToken cancellationToken = default) =>
        Ok(await consultas.ListarAsync(incluirAnuladas, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detalle([FromRoute, System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)] int id,
        CancellationToken cancellationToken)
    {
        var venta = await consultas.ObtenerDetalleAsync(id, cancellationToken);
        return venta is null ? Problem(statusCode: 404, title: "Venta no encontrada.") : Ok(venta);
    }

    [HttpGet("total-dia")]
    public async Task<IActionResult> TotalDia(CancellationToken cancellationToken) =>
        Ok(new { total = await consultas.ObtenerVentasDelDiaAsync(cancellationToken) });
}
