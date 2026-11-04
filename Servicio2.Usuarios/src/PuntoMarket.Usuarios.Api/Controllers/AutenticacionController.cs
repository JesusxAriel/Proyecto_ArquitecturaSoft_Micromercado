using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PuntoMarket.Usuarios.Api.Contracts.Autenticacion;
using PuntoMarket.Usuarios.Application.UseCases.Autenticacion.IniciarSesion;

namespace PuntoMarket.Usuarios.Api.Controllers;

[ApiController]
[Route("api/autenticacion")]
[AllowAnonymous]
public sealed class AutenticacionController(
    IniciarSesionCasoDeUso iniciarSesion,
    ILogger<AutenticacionController> logger) : ControllerBase
{
    [HttpPost("login")]
    [ProducesResponseType<IniciarSesionRespuesta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IniciarSesionRespuesta>> IniciarSesion(
        [FromBody] IniciarSesionSolicitud solicitud,
        CancellationToken cancellationToken)
    {
        try
        {
            var salida = await iniciarSesion.EjecutarAsync(
                new IniciarSesionEntrada(solicitud.Correo, solicitud.Contrasena),
                cancellationToken);

            return Ok(new IniciarSesionRespuesta(salida.Token));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Correo o contraseña incorrectos."
            });
        }
        catch (ArgumentException)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Ingrese un correo y una contraseña válidos."
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error inesperado durante el inicio de sesión.");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "No se pudo completar el inicio de sesión."
            });
        }
    }
}
