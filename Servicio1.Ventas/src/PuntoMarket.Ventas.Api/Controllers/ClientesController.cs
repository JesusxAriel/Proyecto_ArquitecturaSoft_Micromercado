using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PuntoMarket.Ventas.Api.Security;
using PuntoMarket.Ventas.Application.Customers;

namespace PuntoMarket.Ventas.Api.Controllers;

[ApiController, Route("api/clientes")]
[Authorize(Policy = SeguridadJwt.OperarVentas)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ClientesController(
    IClienteVentaConsultaService consultas,
    IClienteCreacionService creacion) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Buscar([FromQuery, StringLength(150)] string? termino = null, CancellationToken cancellationToken = default) =>
        Ok(await consultas.BuscarAsync(termino, cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Crear(ClienteCreacionDto solicitud, CancellationToken cancellationToken)
    {
        var cliente = await creacion.CrearAsync(solicitud, cancellationToken);
        return CreatedAtAction(nameof(Buscar), new { termino = cliente.NitCi }, cliente);
    }
}
