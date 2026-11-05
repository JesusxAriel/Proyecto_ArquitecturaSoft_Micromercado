using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PuntoMarket.Usuarios.Api.Contracts.Autenticacion;
using PuntoMarket.Usuarios.Application.UseCases.Autenticacion.CambiarPassword;
using PuntoMarket.Usuarios.Application.UseCases.Autenticacion.IniciarSesion;

namespace PuntoMarket.Usuarios.Api.Controllers;

[ApiController]
[Route("api/autenticacion")]
public sealed class AutenticacionController(
    IniciarSesionCasoDeUso iniciarSesion,
    CambiarPasswordCasoDeUso cambiarPassword,
    ILogger<AutenticacionController> logger) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
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

    [HttpPost("cambiar-password")]
    [Authorize]
    [ProducesResponseType<CambiarPasswordRespuesta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CambiarPasswordRespuesta>> CambiarPassword(
        [FromBody] CambiarPasswordSolicitud solicitud,
        CancellationToken cancellationToken)
    {
        try
        {
            var salida = await cambiarPassword.EjecutarAsync(
                new CambiarPasswordEntrada(solicitud.ContrasenaActual, solicitud.NuevaContrasena),
                cancellationToken);

            return Ok(new CambiarPasswordRespuesta(salida.Cambiada));
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "La cuenta no está activa."
            });
        }
        catch (ArgumentException exception)
        {
            var mensaje = exception.ParamName switch
            {
                nameof(CambiarPasswordEntrada.ContrasenaActual) =>
                    string.IsNullOrWhiteSpace(solicitud.ContrasenaActual)
                        ? "La contraseña actual es obligatoria."
                        : "La contraseña actual es incorrecta.",
                nameof(CambiarPasswordEntrada.NuevaContrasena) =>
                    "La nueva contraseña no cumple con los requisitos de seguridad.",
                _ => "La solicitud de cambio de contraseña no es válida."
            };
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = mensaje
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error inesperado durante el cambio de contraseña.");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "No se pudo completar el cambio de contraseña."
            });
        }
    }
}
