using PuntoMarket.Reportes.Application.Common;
using PuntoMarket.Reportes.Application.UseCases.VentasPorCategoria.GenerarVentasPorCategoria;
using PuntoMarket.Reportes.Domain.Exceptions;
using PuntoMarket.Reportes.Domain.ValueObjects;
using PuntoMarket.Reportes.Domain.VentasPorCategoria;

namespace PuntoMarket.Reportes.Tests.VentasPorCategoria;

public class GenerarVentasPorCategoriaCasoDeUsoTests
{
    private readonly ConsultaFalsa _consulta = new(
        new FilaDeCategoria(1, "Bebidas", 10, 100m),
        new FilaDeCategoria(2, "Lácteos", 4, 60m));

    private readonly ConsultaDeTendenciaFalsa _tendencia = new(
        new PuntoDeTendencia(new DateOnly(2026, 5, 1), 40m),
        new PuntoDeTendencia(new DateOnly(2026, 5, 3), 120m));

    private readonly ConsultaDeProductosFalsa _productos = new(
        new FilaDeProducto(9, "Leche entera 1 L", 6, 39m));

    private readonly ExportadorFalso _exportadorPdf = new(FormatoDeExportacion.Pdf);

    private GenerarVentasPorCategoriaCasoDeUso CrearCasoDeUso() =>
        new(_consulta, _tendencia, _productos, new CreadorFalso(_exportadorPdf), new RelojFalso(), new UsuarioFalso());

    private static GenerarVentasPorCategoriaEntrada Entrada(
        DateOnly? desde = null,
        DateOnly? hasta = null,
        int? idCategoria = null,
        int? top = null,
        FormatoDeExportacion formato = FormatoDeExportacion.Pdf) =>
        new(desde, hasta, idCategoria, top, formato);

    [Theory]
    [InlineData(FormatoDeExportacion.Json)]
    [InlineData(FormatoDeExportacion.Pdf)]
    [InlineData(FormatoDeExportacion.Xlsx)]
    public async Task Top_limita_categorias_pero_no_totales_del_periodo(FormatoDeExportacion formato)
    {
        var exportador = new ExportadorFalso(formato);
        var caso = new GenerarVentasPorCategoriaCasoDeUso(_consulta, _tendencia, _productos, new CreadorFalso(exportador), new RelojFalso(), new UsuarioFalso());
        var salida = await caso.EjecutarAsync(Entrada(new(2026, 1, 1), new(2026, 1, 31), top: 1, formato: formato), default);
        Assert.Single(salida.Reporte.Filas);
        Assert.Equal(14, salida.Reporte.Totales.TotalUnidades);
        Assert.Equal(160m, salida.Reporte.Totales.TotalMonto);
        Assert.Equal("Bebidas", salida.Reporte.Totales.CategoriaLider!.NombreCategoria);
        Assert.Equal(1, _consulta.TopRecibido);
        Assert.Equal(new DateOnly(2026, 1, 31), _consulta.RangoRecibido!.Hasta);
        if (formato != FormatoDeExportacion.Json) Assert.Same(salida.Reporte, exportador.ReporteRecibido);
    }

    [Fact]
    public async Task Al_exportar_agrega_la_tendencia_y_los_productos_de_las_categorias_del_reporte()
    {
        await CrearCasoDeUso().EjecutarAsync(Entrada(), CancellationToken.None);

        Assert.Equal([1, 2], _tendencia.CategoriasRecibidas);
        Assert.Equal(GenerarVentasPorCategoriaCasoDeUso.ProductosMasVendidosMaximo, _productos.LimiteRecibido);
        var reporte = _exportadorPdf.ReporteRecibido!;
        Assert.Equal(3, reporte.Tendencia!.Puntos.Count);
        Assert.Single(reporte.ProductosMasVendidos!);
    }

    [Fact]
    public async Task En_formato_json_no_consulta_la_tendencia_ni_los_productos()
    {
        await CrearCasoDeUso().EjecutarAsync(Entrada(formato: FormatoDeExportacion.Json), CancellationToken.None);

        Assert.Equal(0, _tendencia.Llamadas);
        Assert.Equal(0, _productos.Llamadas);
    }

    [Fact]
    public async Task Sin_categorias_no_consulta_la_tendencia_ni_los_productos()
    {
        var vacio = new GenerarVentasPorCategoriaCasoDeUso(
            new ConsultaFalsa(), _tendencia, _productos, new CreadorFalso(_exportadorPdf), new RelojFalso(), new UsuarioFalso());

        await vacio.EjecutarAsync(Entrada(), CancellationToken.None);

        Assert.Equal(0, _tendencia.Llamadas);
        Assert.Equal(0, _productos.Llamadas);
    }

    [Fact]
    public async Task Un_rango_invertido_lanza_excepcion_y_no_consulta()
    {
        var entrada = Entrada(new DateOnly(2026, 2, 1), new DateOnly(2026, 1, 1));

        await Assert.ThrowsAsync<RangoDeFechasInvalidoException>(
            () => CrearCasoDeUso().EjecutarAsync(entrada, CancellationToken.None));

        Assert.Equal(0, _consulta.Llamadas);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    [InlineData(GenerarVentasPorCategoriaCasoDeUso.TopMaximo + 1)]
    public async Task Un_top_fuera_de_rango_lanza_excepcion(int top)
    {
        await Assert.ThrowsAsync<FiltroDeReporteInvalidoException>(
            () => CrearCasoDeUso().EjecutarAsync(Entrada(top: top), CancellationToken.None));
    }

    [Fact]
    public async Task Un_identificador_de_categoria_no_positivo_lanza_excepcion()
    {
        await Assert.ThrowsAsync<FiltroDeReporteInvalidoException>(
            () => CrearCasoDeUso().EjecutarAsync(Entrada(idCategoria: 0), CancellationToken.None));
    }

    [Fact]
    public async Task Con_formato_pdf_consulta_arma_el_reporte_y_devuelve_el_archivo()
    {
        var entrada = Entrada(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), 2, 5);

        var salida = await CrearCasoDeUso().EjecutarAsync(entrada, CancellationToken.None);

        Assert.Equal("reporte.Pdf", salida.Archivo?.NombreDeArchivo);
        Assert.Equal(new DateOnly(2026, 1, 1), _consulta.RangoRecibido?.Desde);
        Assert.Equal(2, _consulta.IdCategoriaRecibido);
        Assert.Equal(5, _consulta.TopRecibido);
        Assert.Equal(RelojFalso.Momento, _exportadorPdf.ReporteRecibido?.GeneradoEn);
        Assert.Equal("ariana", _exportadorPdf.ReporteRecibido?.GeneradoPor);
        Assert.Equal(160m, salida.Reporte.Totales.TotalMonto);
    }

    [Fact]
    public async Task Con_formato_json_devuelve_los_datos_sin_archivo()
    {
        var salida = await CrearCasoDeUso().EjecutarAsync(Entrada(formato: FormatoDeExportacion.Json), CancellationToken.None);

        Assert.Null(salida.Archivo);
        Assert.Equal(2, salida.Reporte.Filas.Count);
        Assert.Equal("Bebidas", salida.Reporte.Totales.CategoriaLider?.NombreCategoria);
    }

    [Fact]
    public async Task Un_formato_no_soportado_lanza_excepcion()
    {
        await Assert.ThrowsAsync<FormatoDeExportacionNoSoportadoException>(
            () => CrearCasoDeUso().EjecutarAsync(Entrada(formato: FormatoDeExportacion.Xlsx), CancellationToken.None));
    }
}
