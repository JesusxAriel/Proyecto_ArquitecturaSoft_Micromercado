using Microsoft.AspNetCore.Mvc;
using Proyecto_Arquitectura_Micromercado.Application.Common;

namespace Proyecto_Arquitectura_Micromercado.Pages.Shared;

public sealed class EjecutorDePeticiones(ILogger logger)
{
    public async Task<IActionResult> EjecutarAsync(
        CancellationToken peticion,
        TimeSpan tiempoMaximo,
        Func<CancellationToken, Task<IActionResult>> operacion,
        string descripcion,
        object cuerpoDeFallo)
    {
        if (peticion.IsCancellationRequested)
        {
            return new EmptyResult();
        }

        using var limite = new CancellationTokenSource(tiempoMaximo);
        try
        {
            var resultado = await operacion(limite.Token);
            return peticion.IsCancellationRequested ? new EmptyResult() : resultado;
        }
        catch (OperationCanceledException) when (peticion.IsCancellationRequested)
        {
            return new EmptyResult();
        }
        catch (Exception ex)
        {
            return Fallo(ex, descripcion, cuerpoDeFallo);
        }
    }

    private JsonResult Fallo(Exception ex, string descripcion, object cuerpo)
    {
        if (ex is OperationCanceledException)
        {
            logger.LogWarning("La operación sobre {Descripcion} superó el tiempo máximo.", descripcion);
        }
        else if (ex is ErrorDePersistenciaException)
        {
            logger.LogWarning(ex, "No se pudo completar la operación sobre {Descripcion}.", descripcion);
        }
        else
        {
            logger.LogError(ex, "Error inesperado en la operación sobre {Descripcion}.", descripcion);
        }

        return new JsonResult(cuerpo)
        {
            StatusCode = StatusCodes.Status503ServiceUnavailable
        };
    }
}
