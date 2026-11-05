using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PuntoMarket.Ventas.Api.Security;
using PuntoMarket.Ventas.Application.Comprobantes;

namespace PuntoMarket.Ventas.Api.Controllers;

[ApiController, Route("api/ventas/{id:int}/comprobante")]
[Authorize(Policy = SeguridadJwt.OperarVentas)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ComprobantesController(IComprobanteService comprobantes, IGeneradorComprobante generador) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Consultar([Range(1, int.MaxValue)] int id, CancellationToken cancellationToken) =>
        await comprobantes.ObtenerAsync(id, cancellationToken) is { } comprobante ? Ok(comprobante) : NoEncontrado();

    [HttpGet("html")]
    public async Task<IActionResult> Html([Range(1, int.MaxValue)] int id, CancellationToken cancellationToken)
    {
        var comprobante = await comprobantes.ObtenerAsync(id, cancellationToken);
        return comprobante is null ? NoEncontrado() : Content(generador.GenerarHtml(comprobante), "text/html; charset=utf-8");
    }

    private ObjectResult NoEncontrado() => Problem(statusCode: 404, title: "Comprobante no encontrado.");
}
