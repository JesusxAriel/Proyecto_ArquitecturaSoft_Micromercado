using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Proyecto_Arquitectura_Micromercado.Application.Common;
using Proyecto_Arquitectura_Micromercado.Application.Sales;
using Proyecto_Arquitectura_Micromercado.Pages.Sales;
using System.Text.Json;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class SalesHistorialHandlerTests
{
    private static readonly DateTime Fecha = new(2026, 6, 15, 10, 30, 0);

    [Fact]
    public async Task HistorialDevuelveLasVentasYPasaElFiltro()
    {
        var servicio = new ConsultaPrueba();
        var page = CrearPagina(servicio);

        var resultado = Assert.IsType<JsonResult>(await page.OnGetHistorialAsync(true, default));
        var json = JsonSerializer.SerializeToElement(resultado.Value);

        Assert.True(servicio.IncluirAnuladasRecibido);
        var venta = Assert.Single(json.GetProperty("ventas").EnumerateArray());
        Assert.Equal(7, venta.GetProperty("id").GetInt32());
        Assert.Equal("V-20260615-0007", venta.GetProperty("nroComprobante").GetString());
        Assert.Equal("Cliente Uno", venta.GetProperty("nombreCliente").GetString());
        Assert.Equal("COMPLETADA", venta.GetProperty("estado").GetString());
        Assert.Equal(40m, venta.GetProperty("total").GetDecimal());
        Assert.Equal("no-store", page.Response.Headers.CacheControl.ToString());
    }

    [Fact]
    public async Task DetalleDevuelveLosDatosCompletos()
    {
        var page = CrearPagina(new ConsultaPrueba());

        var resultado = Assert.IsType<JsonResult>(await page.OnGetDetalleAsync(7, default));
        var json = JsonSerializer.SerializeToElement(resultado.Value);

        Assert.Equal("V-20260615-0007", json.GetProperty("nroComprobante").GetString());
        Assert.Equal("Cliente Uno", json.GetProperty("razonSocialCliente").GetString());
        Assert.Equal("Cajero Uno", json.GetProperty("usuarioCajero").GetString());
        Assert.Equal(50m, json.GetProperty("efectivoPagado").GetDecimal());
        Assert.Equal(10m, json.GetProperty("cambio").GetDecimal());
        var linea = Assert.Single(json.GetProperty("lineas").EnumerateArray());
        Assert.Equal(4, linea.GetProperty("cantidad").GetInt32());
        Assert.Equal("Leche", linea.GetProperty("descripcion").GetString());
        var lotes = linea.GetProperty("lotes").EnumerateArray().ToArray();
        Assert.Equal(2, lotes.Length);
        Assert.Equal("LOT-A", lotes[0].GetProperty("codigoLote").GetString());
        Assert.Equal(3, lotes[0].GetProperty("cantidad").GetInt32());
    }

    [Fact]
    public async Task DetalleInexistenteDevuelve404()
    {
        var page = CrearPagina(new ConsultaPrueba());

        var resultado = Assert.IsType<JsonResult>(await page.OnGetDetalleAsync(99, default));
        var json = JsonSerializer.SerializeToElement(resultado.Value);

        Assert.Equal(404, resultado.StatusCode);
        Assert.Equal("La venta solicitada no existe.", json.GetProperty("mensaje").GetString());
    }

    [Fact]
    public async Task ErrorDePersistenciaEnHistorialDevuelve503()
    {
        var page = CrearPagina(new ConsultaPrueba { Error = new ErrorDePersistenciaException(new Exception("interno")) });

        var resultado = Assert.IsType<JsonResult>(await page.OnGetHistorialAsync(false, default));

        Assert.Equal(503, resultado.StatusCode);
        Assert.DoesNotContain("interno", JsonSerializer.Serialize(resultado.Value));
    }

    [Fact]
    public async Task ErrorDePersistenciaEnDetalleDevuelve503()
    {
        var page = CrearPagina(new ConsultaPrueba { Error = new ErrorDePersistenciaException(new Exception("interno")) });

        var resultado = Assert.IsType<JsonResult>(await page.OnGetDetalleAsync(7, default));

        Assert.Equal(503, resultado.StatusCode);
    }

    private static CancellationToken Cancelado()
    {
        var origen = new CancellationTokenSource();
        origen.Cancel();
        return origen.Token;
    }

    [Fact]
    public async Task Con_la_peticion_ya_cancelada_el_historial_no_consulta_y_no_devuelve_nada()
    {
        var servicio = new ConsultaPrueba();
        var registro = new RegistroEspia();
        var page = CrearPagina(servicio, registro);

        var resultado = await page.OnGetHistorialAsync(false, Cancelado());

        Assert.IsType<EmptyResult>(resultado);
        Assert.Equal(0, servicio.Llamadas);
        Assert.Empty(registro.Entradas);
    }

    [Fact]
    public async Task Si_la_peticion_se_cancela_durante_el_historial_no_hay_500_ni_logs()
    {
        var origen = new CancellationTokenSource();
        var servicio = new ConsultaPrueba { Antes = () => origen.Cancel(), Error = new TaskCanceledException() };
        var registro = new RegistroEspia();
        var page = CrearPagina(servicio, registro);

        var resultado = await page.OnGetHistorialAsync(false, origen.Token);

        Assert.IsType<EmptyResult>(resultado);
        Assert.Empty(registro.Entradas);
    }

    [Fact]
    public async Task Si_la_peticion_se_cancela_mientras_responde_el_servicio_el_historial_no_se_serializa()
    {
        var origen = new CancellationTokenSource();
        var servicio = new ConsultaPrueba { Antes = () => origen.Cancel() };
        var page = CrearPagina(servicio);

        var resultado = await page.OnGetHistorialAsync(false, origen.Token);

        Assert.IsType<EmptyResult>(resultado);
    }

    [Fact]
    public async Task El_token_de_la_peticion_no_llega_a_la_consulta_del_historial()
    {
        var origen = new CancellationTokenSource();
        var servicio = new ConsultaPrueba();
        var page = CrearPagina(servicio);

        await page.OnGetHistorialAsync(false, origen.Token);
        origen.Cancel();

        Assert.NotNull(servicio.TokenRecibido);
        Assert.NotEqual(origen.Token, servicio.TokenRecibido);
        Assert.False(servicio.TokenRecibido!.Value.IsCancellationRequested);
    }

    [Fact]
    public async Task El_vencimiento_del_tiempo_en_el_historial_devuelve_503_y_un_aviso()
    {
        var servicio = new ConsultaPrueba { Error = new TaskCanceledException() };
        var registro = new RegistroEspia();
        var page = CrearPagina(servicio, registro);

        var resultado = Assert.IsType<JsonResult>(await page.OnGetHistorialAsync(false, default));
        var json = JsonSerializer.SerializeToElement(resultado.Value);

        Assert.Equal(503, resultado.StatusCode);
        Assert.Equal("No se pudo cargar el historial de ventas. Intente nuevamente.", json.GetProperty("mensaje").GetString());
        Assert.Equal([LogLevel.Warning], registro.Niveles);
    }

    [Fact]
    public async Task Una_excepcion_inesperada_en_el_historial_devuelve_503_y_se_registra_como_error()
    {
        var servicio = new ConsultaPrueba { Error = new InvalidOperationException("Información interna") };
        var registro = new RegistroEspia();
        var page = CrearPagina(servicio, registro);

        var resultado = Assert.IsType<JsonResult>(await page.OnGetHistorialAsync(false, default));

        Assert.Equal(503, resultado.StatusCode);
        Assert.DoesNotContain("Información interna", JsonSerializer.Serialize(resultado.Value));
        Assert.Equal([LogLevel.Error], registro.Niveles);
    }

    [Fact]
    public async Task Con_la_peticion_ya_cancelada_el_detalle_no_consulta_y_no_devuelve_nada()
    {
        var servicio = new ConsultaPrueba();
        var registro = new RegistroEspia();
        var page = CrearPagina(servicio, registro);

        var resultado = await page.OnGetDetalleAsync(7, Cancelado());

        Assert.IsType<EmptyResult>(resultado);
        Assert.Equal(0, servicio.Llamadas);
        Assert.Empty(registro.Entradas);
    }

    [Fact]
    public async Task Si_la_peticion_se_cancela_durante_el_detalle_no_hay_500_ni_logs()
    {
        var origen = new CancellationTokenSource();
        var servicio = new ConsultaPrueba { Antes = () => origen.Cancel(), Error = new OperationCanceledException() };
        var registro = new RegistroEspia();
        var page = CrearPagina(servicio, registro);

        var resultado = await page.OnGetDetalleAsync(7, origen.Token);

        Assert.IsType<EmptyResult>(resultado);
        Assert.Empty(registro.Entradas);
    }

    [Fact]
    public async Task Si_la_peticion_se_cancela_mientras_responde_el_servicio_el_detalle_no_se_serializa()
    {
        var origen = new CancellationTokenSource();
        var servicio = new ConsultaPrueba { Antes = () => origen.Cancel() };
        var page = CrearPagina(servicio);

        var resultado = await page.OnGetDetalleAsync(7, origen.Token);

        Assert.IsType<EmptyResult>(resultado);
    }

    [Fact]
    public async Task El_token_de_la_peticion_no_llega_a_la_consulta_del_detalle()
    {
        var origen = new CancellationTokenSource();
        var servicio = new ConsultaPrueba();
        var page = CrearPagina(servicio);

        await page.OnGetDetalleAsync(7, origen.Token);
        origen.Cancel();

        Assert.NotNull(servicio.TokenRecibido);
        Assert.NotEqual(origen.Token, servicio.TokenRecibido);
        Assert.False(servicio.TokenRecibido!.Value.IsCancellationRequested);
    }

    [Fact]
    public async Task El_vencimiento_del_tiempo_en_el_detalle_devuelve_503_y_un_aviso()
    {
        var servicio = new ConsultaPrueba { Error = new TaskCanceledException() };
        var registro = new RegistroEspia();
        var page = CrearPagina(servicio, registro);

        var resultado = Assert.IsType<JsonResult>(await page.OnGetDetalleAsync(7, default));
        var json = JsonSerializer.SerializeToElement(resultado.Value);

        Assert.Equal(503, resultado.StatusCode);
        Assert.Equal("No se pudo cargar el detalle de la venta. Intente nuevamente.", json.GetProperty("mensaje").GetString());
        Assert.Equal([LogLevel.Warning], registro.Niveles);
    }

    [Fact]
    public async Task Una_excepcion_inesperada_en_el_detalle_devuelve_503_y_se_registra_como_error()
    {
        var servicio = new ConsultaPrueba { Error = new InvalidOperationException("Información interna") };
        var registro = new RegistroEspia();
        var page = CrearPagina(servicio, registro);

        var resultado = Assert.IsType<JsonResult>(await page.OnGetDetalleAsync(7, default));

        Assert.Equal(503, resultado.StatusCode);
        Assert.DoesNotContain("Información interna", JsonSerializer.Serialize(resultado.Value));
        Assert.Equal([LogLevel.Error], registro.Niveles);
    }

    private static IndexModel CrearPagina(ConsultaPrueba servicio, RegistroEspia? registro = null) =>
        new(null!, null!, null!, servicio, null!, null!, registro ?? new RegistroEspia())
        {
            PageContext = new PageContext { HttpContext = new DefaultHttpContext() }
        };

    private sealed class ConsultaPrueba : IVentaConsultaService
    {
        public bool? IncluirAnuladasRecibido { get; private set; }
        public CancellationToken? TokenRecibido { get; private set; }
        public int Llamadas { get; private set; }
        public Exception? Error { get; init; }
        public Action? Antes { get; init; }

        public Task<decimal> ObtenerVentasDelDiaAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<VentaResumenDto>> ListarAsync(
            bool incluirAnuladas, CancellationToken cancellationToken = default)
        {
            IncluirAnuladasRecibido = incluirAnuladas;
            TokenRecibido = cancellationToken;
            Llamadas++;
            Antes?.Invoke();
            if (Error is not null) return Task.FromException<IReadOnlyList<VentaResumenDto>>(Error);

            IReadOnlyList<VentaResumenDto> ventas =
                [new(7, "V-20260615-0007", Fecha, "Cliente Uno", "COMPLETADA", 40m)];
            return Task.FromResult(ventas);
        }

        public Task<VentaDetalleDto?> ObtenerDetalleAsync(
            int idVenta, CancellationToken cancellationToken = default)
        {
            TokenRecibido = cancellationToken;
            Llamadas++;
            Antes?.Invoke();
            if (Error is not null) return Task.FromException<VentaDetalleDto?>(Error);
            if (idVenta != 7) return Task.FromResult<VentaDetalleDto?>(null);

            return Task.FromResult<VentaDetalleDto?>(new VentaDetalleDto(
                7, "V-20260615-0007", Fecha, "123", "Cliente Uno", "Cajero Uno", "COMPLETADA",
                40m, 50m, 10m, null, null, null,
                [new LineaDetalleVentaDto(4, "Leche", 10m, 40m, [new LoteConsumidoDto("LOT-A", 3), new LoteConsumidoDto("LOT-B", 1)])]));
        }
    }
}
