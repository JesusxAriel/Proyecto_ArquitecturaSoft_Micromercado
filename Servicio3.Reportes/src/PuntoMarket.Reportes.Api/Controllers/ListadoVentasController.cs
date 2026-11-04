using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PuntoMarket.Reportes.Api.Contracts.ListadoVentas;
using PuntoMarket.Reportes.Application.Common;
using PuntoMarket.Reportes.Application.UseCases.ListadoVentas.GenerarListadoVentas;
using PuntoMarket.Reportes.Domain.Exceptions;

namespace PuntoMarket.Reportes.Api.Controllers;

[ApiController]
[Route("api/reportes/ventas")]
[Authorize(Roles = "Administrador")]
public class ListadoVentasController : ControllerBase
{
    private readonly GenerarListadoVentasCasoDeUso _casoDeUso;

    public ListadoVentasController(GenerarListadoVentasCasoDeUso casoDeUso)
    {
        _casoDeUso = casoDeUso;
    }

    [HttpGet]
    public async Task<IActionResult> Obtener([FromQuery] PeticionListadoVentas peticion, CancellationToken cancelacion)
    {
        try
        {
            var salida = await _casoDeUso.EjecutarAsync(peticion.AEntrada(), cancelacion);

            return ConvertirRespuesta(salida);
        }
        catch (Exception excepcion) when (EsFiltroInvalido(excepcion))
        {
            return Problem(detail: excepcion.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private IActionResult ConvertirRespuesta(GenerarListadoVentasSalida salida) =>
        salida.Archivo is { } archivo
            ? File(archivo.Contenido, archivo.TipoDeContenido, archivo.NombreDeArchivo)
            : Ok(RespuestaListadoVentas.Desde(salida.Reporte));

    private static bool EsFiltroInvalido(Exception excepcion) =>
        excepcion is RangoDeFechasInvalidoException
            or FiltroDeReporteInvalidoException
            or FormatoDeExportacionNoSoportadoException;
}
