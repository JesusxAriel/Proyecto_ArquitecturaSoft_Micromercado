using PuntoMarket.Reportes.Application.Common;
using PuntoMarket.Reportes.Application.UseCases.ListadoVentas.GenerarListadoVentas;
using PuntoMarket.Reportes.Domain.Exceptions;
using PuntoMarket.Reportes.Domain.ListadoVentas;

namespace PuntoMarket.Reportes.Tests.ListadoVentas;

public class GenerarListadoVentasCasoDeUsoTests
{
    private readonly ConsultaFalsa _consulta = new(
        Fila(1, EstadoDeVenta.Completada, 120m),
        Fila(2, EstadoDeVenta.Completada, 80m),
        Fila(3, EstadoDeVenta.Anulada, 50m));

    private readonly ExportadorFalso _exportadorPdf = new(FormatoDeExportacion.Pdf);

    private static FilaDeVenta Fila(int id, EstadoDeVenta estado, decimal monto) =>
        new(id, $"F-{id:0000}", new DateTime(2026, 1, 15, 10, 0, 0), "1234567", "Comercial Andina", "ventas01", estado, monto);

    private GenerarListadoVentasCasoDeUso CrearCasoDeUso() => CrearCasoDeUso(_consulta);

    private GenerarListadoVentasCasoDeUso CrearCasoDeUso(ConsultaFalsa consulta) =>
        new(consulta, new CreadorFalso(_exportadorPdf), new RelojFalso(), new UsuarioFalso());

    private static GenerarListadoVentasEntrada Entrada(
        DateOnly? desde = null,
        DateOnly? hasta = null,
        EstadoDeVenta? estado = null,
        string? cliente = null,
        int? tope = null,
        FormatoDeExportacion formato = FormatoDeExportacion.Pdf) =>
        new(desde, hasta, estado, cliente, tope, formato);

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
    [InlineData(-5)]
    [InlineData(GenerarListadoVentasCasoDeUso.TopeMaximo + 1)]
    public async Task Un_tope_fuera_de_rango_lanza_excepcion(int tope)
    {
        await Assert.ThrowsAsync<FiltroDeReporteInvalidoException>(
            () => CrearCasoDeUso().EjecutarAsync(Entrada(tope: tope), CancellationToken.None));

        Assert.Equal(0, _consulta.Llamadas);
    }

    [Fact]
    public async Task Un_cliente_demasiado_corto_lanza_excepcion()
    {
        await Assert.ThrowsAsync<FiltroDeReporteInvalidoException>(
            () => CrearCasoDeUso().EjecutarAsync(Entrada(cliente: "A"), CancellationToken.None));
    }

    [Fact]
    public async Task Un_estado_fuera_de_la_enumeracion_lanza_excepcion()
    {
        var entrada = Entrada(estado: (EstadoDeVenta)9);

        await Assert.ThrowsAsync<FiltroDeReporteInvalidoException>(
            () => CrearCasoDeUso().EjecutarAsync(entrada, CancellationToken.None));
    }

    [Fact]
    public async Task Un_rango_sin_datos_devuelve_el_reporte_vacio_con_totales_en_cero()
    {
        var entrada = Entrada(new DateOnly(2020, 1, 1), new DateOnly(2020, 1, 31));

        var salida = await CrearCasoDeUso(new ConsultaFalsa()).EjecutarAsync(entrada, CancellationToken.None);

        Assert.Empty(salida.Reporte.Filas);
        Assert.Equal(0, salida.Reporte.Totales.CantidadDeCompletadas);
        Assert.Equal(0m, salida.Reporte.Totales.MontoTotalDeCompletadas);
        Assert.Equal(0m, salida.Reporte.Totales.PromedioPorVenta);
        Assert.NotNull(salida.Archivo);
    }

    [Fact]
    public async Task Con_formato_pdf_consulta_arma_el_reporte_y_devuelve_el_archivo()
    {
        var entrada = Entrada(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31),
            EstadoDeVenta.Completada,
            " Comercial Andina ",
            50);

        var salida = await CrearCasoDeUso().EjecutarAsync(entrada, CancellationToken.None);

        Assert.Equal("reporte.Pdf", salida.Archivo?.NombreDeArchivo);
        Assert.Equal(new DateOnly(2026, 1, 1), _consulta.RangoRecibido?.Desde);
        Assert.Equal(new DateOnly(2026, 1, 31), _consulta.RangoRecibido?.Hasta);
        Assert.Equal(EstadoDeVenta.Completada, _consulta.EstadoRecibido);
        Assert.Equal("Comercial Andina", _consulta.ClienteRecibido);
        Assert.Equal(50, _consulta.TopeRecibido);
        Assert.Equal(RelojFalso.Momento, _exportadorPdf.ReporteRecibido?.GeneradoEn);
        Assert.Equal("alejandra", _exportadorPdf.ReporteRecibido?.GeneradoPor);
        Assert.Equal(200m, salida.Reporte.Totales.MontoTotalDeCompletadas);
        Assert.Equal(100m, salida.Reporte.Totales.PromedioPorVenta);
    }

    [Fact]
    public async Task Con_formato_json_devuelve_los_datos_sin_archivo()
    {
        var salida = await CrearCasoDeUso().EjecutarAsync(Entrada(formato: FormatoDeExportacion.Json), CancellationToken.None);

        Assert.Null(salida.Archivo);
        Assert.Equal(3, salida.Reporte.Filas.Count);
        Assert.Equal(1, salida.Reporte.Totales.CantidadDeAnuladas);
    }

    [Fact]
    public async Task Sin_filtros_consulta_con_el_tope_maximo_y_sin_cliente()
    {
        await CrearCasoDeUso().EjecutarAsync(Entrada(cliente: "   "), CancellationToken.None);

        Assert.Null(_consulta.ClienteRecibido);
        Assert.Null(_consulta.EstadoRecibido);
        Assert.Null(_consulta.RangoRecibido?.Desde);
        Assert.Equal(GenerarListadoVentasCasoDeUso.TopeMaximo, _consulta.TopeRecibido);
    }

    [Fact]
    public async Task Un_formato_no_soportado_lanza_excepcion()
    {
        await Assert.ThrowsAsync<FormatoDeExportacionNoSoportadoException>(
            () => CrearCasoDeUso().EjecutarAsync(Entrada(formato: FormatoDeExportacion.Xlsx), CancellationToken.None));
    }
}
