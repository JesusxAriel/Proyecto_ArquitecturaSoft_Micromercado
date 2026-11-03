using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PuntoMarket.Usuarios.Api.Contracts.Gestion;
using PuntoMarket.Usuarios.Api.Filters;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.ActivarCuenta;

namespace PuntoMarket.Usuarios.Api.Controllers;

[ApiController]
[Route("api/activacion")]
[AllowAnonymous]
[GestionExceptionFilter(StatusCodes.Status400BadRequest)]
public sealed class ActivacionController(ActivarCuentaCasoDeUso activarCuenta) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ActivarCuentaRespuesta>> Activar(
        [FromBody] ActivarCuentaSolicitud solicitud,
        CancellationToken cancellationToken)
    {
        var salida = await activarCuenta.EjecutarAsync(new ActivarCuentaEntrada(
            solicitud.Token, solicitud.NuevaContrasena, solicitud.ConfirmarContrasena), cancellationToken);
        return Ok(new ActivarCuentaRespuesta(salida.Activada));
    }
}
