using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PuntoMarket.Ventas.Api.Security;
using PuntoMarket.Ventas.Application.Products;

namespace PuntoMarket.Ventas.Api.Controllers;

[ApiController, Route("api/productos")]
[Authorize(Policy = SeguridadJwt.OperarVentas)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ProductosController(IProductoVentaConsultaService consultas) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Buscar([FromQuery, StringLength(150)] string? termino = null, CancellationToken cancellationToken = default) =>
        Ok(await consultas.BuscarAsync(termino, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detalle([Range(1, int.MaxValue)] int id, CancellationToken cancellationToken) =>
        await consultas.ObtenerAsync(id, cancellationToken) is { } producto ? Ok(producto) : NoEncontrado();

    [HttpGet("{id:int}/precio")]
    public async Task<IActionResult> Precio([Range(1, int.MaxValue)] int id, CancellationToken cancellationToken) =>
        await consultas.ObtenerAsync(id, cancellationToken) is { } producto ? Ok(new { producto.Id, producto.PrecioVenta }) : NoEncontrado();

    [HttpGet("{id:int}/stock")]
    public async Task<IActionResult> Stock([Range(1, int.MaxValue)] int id, CancellationToken cancellationToken) =>
        await consultas.ObtenerAsync(id, cancellationToken) is { } producto ? Ok(new { producto.Id, producto.StockDisponible }) : NoEncontrado();

    private ObjectResult NoEncontrado() => Problem(statusCode: 404, title: "Producto inexistente o inactivo.");
}
