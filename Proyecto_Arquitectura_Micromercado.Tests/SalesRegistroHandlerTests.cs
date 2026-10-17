using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Proyecto_Arquitectura_Micromercado.Application.Facades;
using Proyecto_Arquitectura_Micromercado.Application.Sales;
using Proyecto_Arquitectura_Micromercado.Pages.Sales;
using System.Text.Json;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class SalesRegistroHandlerTests
{
    [Fact]
    public async Task RegistroEnviaDetalleALaFachadaYDevuelveSuTotal()
    {
        var fachada = new FachadaPrueba();
        var page = CrearPagina(fachada);
        var resultado = Assert.IsType<JsonResult>(await page.OnPostRegistrarVentaAsync(Entrada(), default));
        var json = JsonSerializer.SerializeToElement(resultado.Value);

        Assert.True(json.GetProperty("exito").GetBoolean());
        Assert.Equal(73, json.GetProperty("idVenta").GetInt32());
        Assert.Equal(19.75m, json.GetProperty("total").GetDecimal());
        Assert.Equal(4, fachada.Recibido!.IdCliente);
        Assert.Equal(25m, fachada.Recibido.EfectivoPagado);
        Assert.Equal(new LineaVentaDto(8, 2, 9.5m), Assert.Single(fachada.Recibido.Lineas));
    }

    [Fact]
    public async Task Un_cambio_de_precio_devuelve_400_con_el_codigo_el_producto_y_el_precio_vigente()
    {
        var fachada = new FachadaPrueba { Error = new PrecioCambiadoException(8, 7m, "El precio cambió.") };
        var page = CrearPagina(fachada);

        var resultado = Assert.IsType<JsonResult>(await page.OnPostRegistrarVentaAsync(Entrada(), default));
        var json = JsonSerializer.SerializeToElement(resultado.Value);

        Assert.Equal(400, resultado.StatusCode);
        Assert.False(json.GetProperty("exito").GetBoolean());
        Assert.Equal("precio-cambiado", json.GetProperty("codigo").GetString());
        Assert.Equal(8, json.GetProperty("idProducto").GetInt32());
        Assert.Equal(7m, json.GetProperty("precioActual").GetDecimal());
        Assert.Equal("El precio cambió.", json.GetProperty("mensaje").GetString());
    }

    [Fact]
    public async Task Una_linea_sin_precio_esperado_devuelve_400_y_no_invoca_la_fachada()
    {
        var fachada = new FachadaPrueba();
        var page = CrearPagina(fachada);
        var entrada = new IndexModel.RegistrarVentaInput
        {
            IdCliente = 4,
            EfectivoPagado = 25m,
            Lineas = [new() { IdProducto = 8, Cantidad = 2 }]
        };

        var resultado = Assert.IsType<JsonResult>(await page.OnPostRegistrarVentaAsync(entrada, default));

        Assert.Equal(400, resultado.StatusCode);
        Assert.Null(fachada.Recibido);
    }

    [Fact]
    public async Task BindingInvalidoNoInvocaLaFachada()
    {
        var fachada = new FachadaPrueba();
        var page = CrearPagina(fachada);
        page.ModelState.AddModelError("venta.Lineas[0].Cantidad", "Valor inválido");

        var resultado = Assert.IsType<JsonResult>(await page.OnPostRegistrarVentaAsync(Entrada(), default));

        Assert.Equal(400, resultado.StatusCode);
        Assert.Null(fachada.Recibido);
    }

    [Fact]
    public async Task ErrorDeNegocioDevuelveMensajeLegible()
    {
        var page = CrearPagina(new FachadaPrueba { Error = new InvalidOperationException("Efectivo insuficiente.") });

        var resultado = Assert.IsType<JsonResult>(await page.OnPostRegistrarVentaAsync(Entrada(), default));
        var json = JsonSerializer.SerializeToElement(resultado.Value);

        Assert.Equal(400, resultado.StatusCode);
        Assert.False(json.GetProperty("exito").GetBoolean());
        Assert.Equal("Efectivo insuficiente.", json.GetProperty("mensaje").GetString());
    }

    [Fact]
    public async Task ErrorInesperadoNoExponeDetallesInternos()
    {
        var page = CrearPagina(new FachadaPrueba { Error = new Exception("Información interna de conexión") });

        var resultado = Assert.IsType<JsonResult>(await page.OnPostRegistrarVentaAsync(Entrada(), default));

        Assert.Equal(503, resultado.StatusCode);
        Assert.DoesNotContain("Información interna", JsonSerializer.Serialize(resultado.Value));
    }

    private static CancellationToken Cancelado()
    {
        var origen = new CancellationTokenSource();
        origen.Cancel();
        return origen.Token;
    }

    [Fact]
    public async Task Con_la_peticion_ya_cancelada_no_se_registra_y_no_se_devuelve_nada()
    {
        var fachada = new FachadaPrueba();
        var registro = new RegistroEspia();
        var page = CrearPagina(fachada, registro);

        var resultado = await page.OnPostRegistrarVentaAsync(Entrada(), Cancelado());

        Assert.IsType<EmptyResult>(resultado);
        Assert.Null(fachada.Recibido);
        Assert.Empty(registro.Entradas);
    }

    [Fact]
    public async Task Si_la_peticion_se_cancela_durante_el_registro_no_hay_500_ni_logs()
    {
        var origen = new CancellationTokenSource();
        var fachada = new FachadaPrueba { Antes = () => origen.Cancel(), Error = new TaskCanceledException() };
        var registro = new RegistroEspia();
        var page = CrearPagina(fachada, registro);

        var resultado = await page.OnPostRegistrarVentaAsync(Entrada(), origen.Token);

        Assert.IsType<EmptyResult>(resultado);
        Assert.Empty(registro.Entradas);
    }

    [Fact]
    public async Task Si_el_cliente_se_va_mientras_se_registra_la_venta_igual_se_completa()
    {
        var origen = new CancellationTokenSource();
        var fachada = new FachadaPrueba { Antes = () => origen.Cancel() };
        var page = CrearPagina(fachada);

        var resultado = await page.OnPostRegistrarVentaAsync(Entrada(), origen.Token);

        Assert.IsType<EmptyResult>(resultado);
        Assert.NotNull(fachada.Recibido);
        Assert.False(fachada.TokenRecibido!.Value.IsCancellationRequested);
    }

    [Fact]
    public async Task El_token_de_la_peticion_no_llega_a_la_fachada_de_venta()
    {
        var origen = new CancellationTokenSource();
        var fachada = new FachadaPrueba();
        var page = CrearPagina(fachada);

        await page.OnPostRegistrarVentaAsync(Entrada(), origen.Token);
        origen.Cancel();

        Assert.NotNull(fachada.TokenRecibido);
        Assert.NotEqual(origen.Token, fachada.TokenRecibido);
        Assert.False(fachada.TokenRecibido!.Value.IsCancellationRequested);
    }

    [Fact]
    public async Task El_vencimiento_del_tiempo_del_registro_devuelve_503_con_json_valido_y_un_aviso()
    {
        var fachada = new FachadaPrueba { Error = new TaskCanceledException() };
        var registro = new RegistroEspia();
        var page = CrearPagina(fachada, registro);

        var resultado = Assert.IsType<JsonResult>(await page.OnPostRegistrarVentaAsync(Entrada(), default));
        var json = JsonSerializer.SerializeToElement(resultado.Value);

        Assert.Equal(503, resultado.StatusCode);
        Assert.False(json.GetProperty("exito").GetBoolean());
        Assert.Equal(
            "No se pudo registrar la venta. Revise el stock disponible y el efectivo pagado, o intente nuevamente.",
            json.GetProperty("mensaje").GetString());
        Assert.Equal([LogLevel.Warning], registro.Niveles);
    }

    [Fact]
    public async Task Una_excepcion_inesperada_en_el_registro_se_registra_como_error()
    {
        var fachada = new FachadaPrueba { Error = new NullReferenceException("Información interna") };
        var registro = new RegistroEspia();
        var page = CrearPagina(fachada, registro);

        var resultado = Assert.IsType<JsonResult>(await page.OnPostRegistrarVentaAsync(Entrada(), default));

        Assert.Equal(503, resultado.StatusCode);
        Assert.DoesNotContain("Información interna", JsonSerializer.Serialize(resultado.Value));
        Assert.Equal([LogLevel.Error], registro.Niveles);
    }

    private static IndexModel CrearPagina(FachadaPrueba fachada, RegistroEspia? registro = null) =>
        new(null!, null!, fachada, null!, null!, null!, registro ?? new RegistroEspia());

    private static IndexModel.RegistrarVentaInput Entrada() => new()
    {
        IdCliente = 4,
        EfectivoPagado = 25m,
        Lineas = [new() { IdProducto = 8, Cantidad = 2, PrecioEsperado = 9.5m }]
    };

    private sealed class FachadaPrueba : IFachadaVenta
    {
        public NuevaVentaDto? Recibido { get; private set; }
        public CancellationToken? TokenRecibido { get; private set; }
        public Exception? Error { get; init; }
        public Action? Antes { get; init; }

        public Task<ResultadoVentaDto> RegistrarVentaAsync(NuevaVentaDto dto, CancellationToken cancellationToken = default)
        {
            Recibido = dto;
            TokenRecibido = cancellationToken;
            Antes?.Invoke();
            return Error is null
                ? Task.FromResult(new ResultadoVentaDto(73, 19.75m))
                : Task.FromException<ResultadoVentaDto>(Error);
        }
    }
}
