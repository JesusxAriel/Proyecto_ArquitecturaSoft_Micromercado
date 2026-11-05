using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PuntoMarket.Reportes.Api.Contracts.VentasPorCategoria;
using PuntoMarket.Reportes.Application.Common;
using PuntoMarket.Reportes.Application.UseCases.VentasPorCategoria.GenerarVentasPorCategoria;
using PuntoMarket.Reportes.Application.UseCases.VentasPorCategoria.ListarCategorias;
using PuntoMarket.Reportes.Domain.Exceptions;

namespace PuntoMarket.Reportes.Api.Controllers;

[ApiController]
[Route("api/reportes/ventas-por-categoria")]
[Authorize(Roles = "Administrador")]
public class VentasPorCategoriaController : ControllerBase
{
    private readonly GenerarVentasPorCategoriaCasoDeUso _casoDeUso;

    private readonly ListarCategoriasCasoDeUso _listarCategorias;

    public VentasPorCategoriaController(
        GenerarVentasPorCategoriaCasoDeUso casoDeUso,
        ListarCategoriasCasoDeUso listarCategorias)
    {
        _casoDeUso = casoDeUso;
        _listarCategorias = listarCategorias;
    }

    [HttpGet("categorias")]
    public async Task<IActionResult> ListarCategorias(CancellationToken cancelacion) =>
        Ok(await _listarCategorias.EjecutarAsync(cancelacion));

    [HttpGet]
    public async Task<IActionResult> Obtener([FromQuery] PeticionVentasPorCategoria peticion, CancellationToken cancelacion)
    {
        try
        {
            var entrada = new GenerarVentasPorCategoriaEntrada(
                peticion.Desde, peticion.Hasta, peticion.IdCategoria, peticion.Top, peticion.Formato);
            var salida = await _casoDeUso.EjecutarAsync(entrada, cancelacion);

            return ConvertirRespuesta(salida);
        }
        catch (Exception excepcion) when (EsFiltroInvalido(excepcion))
        {
            return Problem(detail: excepcion.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private IActionResult ConvertirRespuesta(GenerarVentasPorCategoriaSalida salida) =>
        salida.Archivo is { } archivo
            ? File(archivo.Contenido, archivo.TipoDeContenido, archivo.NombreDeArchivo)
            : Ok(RespuestaVentasPorCategoria.Desde(salida.Reporte));

    private static bool EsFiltroInvalido(Exception excepcion) =>
        excepcion is RangoDeFechasInvalidoException
            or FiltroDeReporteInvalidoException
            or FormatoDeExportacionNoSoportadoException;
}
