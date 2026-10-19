using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Arquitectura_Micromercado.Application.Common;
using Proyecto_Arquitectura_Micromercado.Application.Facades;
using Proyecto_Arquitectura_Micromercado.Pages.Sales;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class SalesAnulacionHandlerTests
{
    [Fact]
    public async Task ElAdministradorAnulaLaVenta()
    {
        var fachada = new AnulacionPrueba();
        var page = CrearPagina(fachada, "Administrador");

        var resultado = Assert.IsType<JsonResult>(await page.OnPostAnularVentaAsync(Entrada(), default));
        var json = JsonSerializer.SerializeToElement(resultado.Value);

        Assert.True(json.GetProperty("exito").GetBoolean());
        Assert.Equal((12, "Error de digitación"), fachada.Recibido);
    }

    [Fact]
    public async Task ElCajeroRecibe403YNoSeLlamaALaFachada()
    {
        var fachada = new AnulacionPrueba();
        var page = CrearPagina(fachada, "Cajero");

        var resultado = Assert.IsType<JsonResult>(await page.OnPostAnularVentaAsync(Entrada(), default));
        var json = JsonSerializer.SerializeToElement(resultado.Value);

        Assert.Equal(403, resultado.StatusCode);
        Assert.False(json.GetProperty("exito").GetBoolean());
        Assert.Equal("Solo el administrador puede anular ventas.", json.GetProperty("mensaje").GetString());
        Assert.Null(fachada.Recibido);
    }

    [Fact]
    public async Task UnaEntradaInvalidaDevuelve400SinLlamarALaFachada()
    {
        var fachada = new AnulacionPrueba();
        var page = CrearPagina(fachada, "Administrador");
        page.ModelState.AddModelError("entrada.Motivo", "Valor inválido");

        var resultado = Assert.IsType<JsonResult>(await page.OnPostAnularVentaAsync(Entrada(), default));

        Assert.Equal(400, resultado.StatusCode);
        Assert.Null(fachada.Recibido);
    }

    [Fact]
    public async Task UnaEntradaNulaDevuelve400()
    {
        var page = CrearPagina(new AnulacionPrueba(), "Administrador");

        var resultado = Assert.IsType<JsonResult>(await page.OnPostAnularVentaAsync(null, default));

        Assert.Equal(400, resultado.StatusCode);
    }

    [Fact]
    public async Task UnaVentaYaAnuladaDevuelve400ConElMensaje()
    {
        var page = CrearPagina(
            new AnulacionPrueba { Error = new InvalidOperationException("La venta no existe o ya fue anulada.") },
            "Administrador");

        var resultado = Assert.IsType<JsonResult>(await page.OnPostAnularVentaAsync(Entrada(), default));
        var json = JsonSerializer.SerializeToElement(resultado.Value);

        Assert.Equal(400, resultado.StatusCode);
        Assert.Equal("La venta no existe o ya fue anulada.", json.GetProperty("mensaje").GetString());
    }

    [Fact]
    public async Task UnErrorDePersistenciaDevuelve503SinDetallesInternos()
    {
        var page = CrearPagina(
            new AnulacionPrueba { Error = new ErrorDePersistenciaException(new Exception("Información interna")) },
            "Administrador");

        var resultado = Assert.IsType<JsonResult>(await page.OnPostAnularVentaAsync(Entrada(), default));

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
    public async Task Con_la_peticion_ya_cancelada_no_se_anula_y_no_se_devuelve_nada()
    {
        var fachada = new AnulacionPrueba();
        var registro = new RegistroEspia();
        var page = CrearPagina(fachada, "Administrador", registro);

        var resultado = await page.OnPostAnularVentaAsync(Entrada(), Cancelado());

        Assert.IsType<EmptyResult>(resultado);
        Assert.Null(fachada.Recibido);
        Assert.Empty(registro.Entradas);
    }

    [Fact]
    public async Task Si_la_peticion_se_cancela_durante_la_anulacion_no_hay_500_ni_logs()
    {
        var origen = new CancellationTokenSource();
        var fachada = new AnulacionPrueba { Antes = () => origen.Cancel(), Error = new TaskCanceledException() };
        var registro = new RegistroEspia();
        var page = CrearPagina(fachada, "Administrador", registro);

        var resultado = await page.OnPostAnularVentaAsync(Entrada(), origen.Token);

        Assert.IsType<EmptyResult>(resultado);
        Assert.Empty(registro.Entradas);
    }

    [Fact]
    public async Task Si_el_cliente_se_va_mientras_se_anula_la_anulacion_igual_se_completa()
    {
        var origen = new CancellationTokenSource();
        var fachada = new AnulacionPrueba { Antes = () => origen.Cancel() };
        var page = CrearPagina(fachada, "Administrador");

        var resultado = await page.OnPostAnularVentaAsync(Entrada(), origen.Token);

        Assert.IsType<EmptyResult>(resultado);
        Assert.Equal((12, "Error de digitación"), fachada.Recibido);
        Assert.False(fachada.TokenRecibido!.Value.IsCancellationRequested);
    }

    [Fact]
    public async Task El_token_de_la_peticion_no_llega_a_la_fachada_de_anulacion()
    {
        var origen = new CancellationTokenSource();
        var fachada = new AnulacionPrueba();
        var page = CrearPagina(fachada, "Administrador");

        await page.OnPostAnularVentaAsync(Entrada(), origen.Token);
        origen.Cancel();

        Assert.NotNull(fachada.TokenRecibido);
        Assert.NotEqual(origen.Token, fachada.TokenRecibido);
        Assert.False(fachada.TokenRecibido!.Value.IsCancellationRequested);
    }

    [Fact]
    public async Task El_vencimiento_del_tiempo_de_la_anulacion_devuelve_503_y_un_aviso()
    {
        var fachada = new AnulacionPrueba { Error = new TaskCanceledException() };
        var registro = new RegistroEspia();
        var page = CrearPagina(fachada, "Administrador", registro);

        var resultado = Assert.IsType<JsonResult>(await page.OnPostAnularVentaAsync(Entrada(), default));
        var json = JsonSerializer.SerializeToElement(resultado.Value);

        Assert.Equal(503, resultado.StatusCode);
        Assert.False(json.GetProperty("exito").GetBoolean());
        Assert.Equal("No se pudo anular la venta. Intente nuevamente.", json.GetProperty("mensaje").GetString());
        Assert.Equal([LogLevel.Warning], registro.Niveles);
    }

    [Fact]
    public async Task Una_excepcion_inesperada_en_la_anulacion_se_registra_como_error()
    {
        var fachada = new AnulacionPrueba { Error = new NullReferenceException("Información interna") };
        var registro = new RegistroEspia();
        var page = CrearPagina(fachada, "Administrador", registro);

        var resultado = Assert.IsType<JsonResult>(await page.OnPostAnularVentaAsync(Entrada(), default));

        Assert.Equal(503, resultado.StatusCode);
        Assert.DoesNotContain("Información interna", JsonSerializer.Serialize(resultado.Value));
        Assert.Equal([LogLevel.Error], registro.Niveles);
    }

    private static HistorialModel CrearPagina(AnulacionPrueba fachada, string rol, RegistroEspia? registro = null)
    {
        var identidad = new ClaimsIdentity([new Claim(ClaimTypes.Role, rol)], "prueba");
        return new HistorialModel(null!, fachada, registro ?? new RegistroEspia())
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identidad) }
            }
        };
    }

    private static HistorialModel.AnularVentaInput Entrada() => new()
    {
        IdVenta = 12,
        Motivo = "Error de digitación"
    };

    private sealed class AnulacionPrueba : IFachadaAnulacion
    {
        public (int IdVenta, string Motivo)? Recibido { get; private set; }
        public CancellationToken? TokenRecibido { get; private set; }
        public Exception? Error { get; init; }
        public Action? Antes { get; init; }

        public Task AnularVentaAsync(int idVenta, string motivo, CancellationToken cancellationToken = default)
        {
            Recibido = (idVenta, motivo);
            TokenRecibido = cancellationToken;
            Antes?.Invoke();
            return Error is null ? Task.CompletedTask : Task.FromException(Error);
        }
    }
}
