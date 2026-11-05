using PuntoMarket.Reportes.Application.Common;
using PuntoMarket.Reportes.Application.Ports.Comunes;
using PuntoMarket.Reportes.Application.Ports.Consultas;
using PuntoMarket.Reportes.Application.Ports.Exportacion;
using PuntoMarket.Reportes.Domain.Tendencias;
using PuntoMarket.Reportes.Domain.ValueObjects;
using PuntoMarket.Reportes.Domain.VentasPorCategoria;

namespace PuntoMarket.Reportes.Application.UseCases.VentasPorCategoria.GenerarVentasPorCategoria;

public class GenerarVentasPorCategoriaCasoDeUso
{
    public const int TopMaximo = 100;
    public const int ProductosMasVendidosMaximo = 5;

    private readonly IConsultaVentasPorCategoria _consulta;
    private readonly IConsultaTendenciaDeVentas _consultaDeTendencia;
    private readonly IConsultaProductosMasVendidos _consultaDeProductos;
    private readonly CreadorDeExportador<ReporteVentasPorCategoria> _creador;
    private readonly IReloj _reloj;
    private readonly IUsuarioActual _usuarioActual;

    public GenerarVentasPorCategoriaCasoDeUso(
        IConsultaVentasPorCategoria consulta,
        IConsultaTendenciaDeVentas consultaDeTendencia,
        IConsultaProductosMasVendidos consultaDeProductos,
        CreadorDeExportador<ReporteVentasPorCategoria> creador,
        IReloj reloj,
        IUsuarioActual usuarioActual)
    {
        _consulta = consulta;
        _consultaDeTendencia = consultaDeTendencia;
        _consultaDeProductos = consultaDeProductos;
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
        if (RequiereDetalle(entrada.Formato, resultado.Filas))
            reporte = await AgregarDetalleAsync(reporte, cancelacion);

        return new GenerarVentasPorCategoriaSalida(reporte, GenerarArchivo(reporte, entrada.Formato));
    }

    private static bool RequiereDetalle(FormatoDeExportacion formato, IReadOnlyList<FilaDeCategoria> filas) =>
        formato != FormatoDeExportacion.Json && filas.Count > 0;

    private async Task<ReporteVentasPorCategoria> AgregarDetalleAsync(
        ReporteVentasPorCategoria reporte,
        CancellationToken cancelacion)
    {
        var rango = reporte.Filtros.Rango;
        var categorias = reporte.Filas.Select(f => f.IdCategoria).ToList();
        var montosPorDia = await _consultaDeTendencia.ConsultarAsync(rango, categorias, cancelacion);
        var productos = await _consultaDeProductos.ConsultarAsync(
            rango, categorias, ProductosMasVendidosMaximo, cancelacion);

        return reporte.ConDetalle(TendenciaDeVentas.Desde(montosPorDia), productos);
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
