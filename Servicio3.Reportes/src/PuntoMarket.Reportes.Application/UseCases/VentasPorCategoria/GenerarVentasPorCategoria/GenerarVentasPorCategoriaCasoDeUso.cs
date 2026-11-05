using PuntoMarket.Reportes.Application.Common;
using PuntoMarket.Reportes.Application.Ports.Comunes;
using PuntoMarket.Reportes.Application.Ports.Consultas;
using PuntoMarket.Reportes.Application.Ports.Exportacion;
using PuntoMarket.Reportes.Domain.ValueObjects;
using PuntoMarket.Reportes.Domain.VentasPorCategoria;

namespace PuntoMarket.Reportes.Application.UseCases.VentasPorCategoria.GenerarVentasPorCategoria;

public class GenerarVentasPorCategoriaCasoDeUso
{
    public const int TopMaximo = 100;

    private readonly IConsultaVentasPorCategoria _consulta;
    private readonly CreadorDeExportador<ReporteVentasPorCategoria> _creador;
    private readonly IReloj _reloj;
    private readonly IUsuarioActual _usuarioActual;

    public GenerarVentasPorCategoriaCasoDeUso(
        IConsultaVentasPorCategoria consulta,
        CreadorDeExportador<ReporteVentasPorCategoria> creador,
        IReloj reloj,
        IUsuarioActual usuarioActual)
    {
        _consulta = consulta;
        _creador = creador;
        _reloj = reloj;
        _usuarioActual = usuarioActual;
    }

    public async Task<GenerarVentasPorCategoriaSalida> EjecutarAsync(
        GenerarVentasPorCategoriaEntrada entrada,
        CancellationToken cancelacion)
    {
        var filtros = ValidarFiltros(entrada);
        var resultado = await _consulta.ConsultarAsync(filtros.Rango, filtros.IdCategoria, filtros.Top, cancelacion);
        var reporte = new ReporteVentasPorCategoria(resultado.Filas, resultado.Totales, filtros, _reloj.Ahora(), _usuarioActual.NombreDeUsuario);

        return new GenerarVentasPorCategoriaSalida(reporte, GenerarArchivo(reporte, entrada.Formato));
    }

    private ArchivoGenerado? GenerarArchivo(ReporteVentasPorCategoria reporte, FormatoDeExportacion formato) =>
        formato == FormatoDeExportacion.Json ? null : _creador.Exportar(reporte, formato);

    private static FiltrosDeVentasPorCategoria ValidarFiltros(GenerarVentasPorCategoriaEntrada entrada)
    {
        ValidarIdCategoria(entrada.IdCategoria);
        ValidarTop(entrada.Top);

        return new FiltrosDeVentasPorCategoria(
            new RangoDeFechas(entrada.Desde, entrada.Hasta),
            entrada.IdCategoria,
            entrada.Top);
    }

    private static void ValidarIdCategoria(int? idCategoria)
    {
        if (idCategoria is <= 0)
            throw new FiltroDeReporteInvalidoException("El identificador de la categoría debe ser un número positivo.");
    }

    private static void ValidarTop(int? top)
    {
        if (top is <= 0 or > TopMaximo)
            throw new FiltroDeReporteInvalidoException($"El valor de «top» debe estar entre 1 y {TopMaximo}.");
    }
}
