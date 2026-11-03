using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PuntoMarket.Usuarios.Domain.Exceptions;

namespace PuntoMarket.Usuarios.Api.Filters;

public sealed class GestionExceptionFilterAttribute(int estadoOperacionInvalida) : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        if (context.Exception is OperationCanceledException)
            return;

        if (context.Exception is UnauthorizedAccessException)
        {
            context.Result = new ForbidResult();
            context.ExceptionHandled = true;
            return;
        }

        var (estado, mensaje) = context.Exception switch
        {
            UsuarioDuplicadoException => (StatusCodes.Status409Conflict, "El usuario ya está registrado."),
            ArgumentException => (StatusCodes.Status400BadRequest, "Los datos de la solicitud no son válidos."),
            InvalidOperationException => (estadoOperacionInvalida,
                estadoOperacionInvalida == StatusCodes.Status400BadRequest
                    ? "No se pudo activar la cuenta. El token no es válido o ha expirado."
                    : "La operación no se puede realizar en el estado actual."),
            _ => (StatusCodes.Status500InternalServerError, "No se pudo completar la operación.")
        };

        if (estado == StatusCodes.Status500InternalServerError)
        {
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILogger<GestionExceptionFilterAttribute>>();
            logger.LogError(context.Exception, "Error inesperado en la API de Gestión.");
        }

        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = estado,
            Title = mensaje
        })
        {
            StatusCode = estado
        };
        context.ExceptionHandled = true;
    }
}
