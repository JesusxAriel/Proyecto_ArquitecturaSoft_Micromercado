using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging.Abstractions;
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

        var resultado = await page.OnPostAnularVentaAsync(Entrada(), default);
        var json = JsonSerializer.SerializeToElement(resultado.Value);

        Assert.True(json.GetProperty("exito").GetBoolean());
        Assert.Equal((12, "Error de digitación"), fachada.Recibido);
    }

    [Fact]
    public async Task ElCajeroRecibe403YNoSeLlamaALaFachada()
    {
        var fachada = new AnulacionPrueba();
        var page = CrearPagina(fachada, "Cajero");

        var resultado = await page.OnPostAnularVentaAsync(Entrada(), default);
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

        var resultado = await page.OnPostAnularVentaAsync(Entrada(), default);

        Assert.Equal(400, resultado.StatusCode);
        Assert.Null(fachada.Recibido);
    }

    [Fact]
    public async Task UnaEntradaNulaDevuelve400()
    {
        var page = CrearPagina(new AnulacionPrueba(), "Administrador");

        var resultado = await page.OnPostAnularVentaAsync(null, default);

        Assert.Equal(400, resultado.StatusCode);
    }

    [Fact]
    public async Task UnaVentaYaAnuladaDevuelve400ConElMensaje()
    {
        var page = CrearPagina(
            new AnulacionPrueba { Error = new InvalidOperationException("La venta no existe o ya fue anulada.") },
            "Administrador");

        var resultado = await page.OnPostAnularVentaAsync(Entrada(), default);
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

        var resultado = await page.OnPostAnularVentaAsync(Entrada(), default);

        Assert.Equal(503, resultado.StatusCode);
        Assert.DoesNotContain("Información interna", JsonSerializer.Serialize(resultado.Value));
    }

    private static IndexModel CrearPagina(AnulacionPrueba fachada, string rol)
    {
        var identidad = new ClaimsIdentity([new Claim(ClaimTypes.Role, rol)], "prueba");
        return new IndexModel(null!, null!, null!, null!, fachada, NullLogger<IndexModel>.Instance)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identidad) }
            }
        };
    }

    private static IndexModel.AnularVentaInput Entrada() => new()
    {
        IdVenta = 12,
        Motivo = "Error de digitación"
    };

    private sealed class AnulacionPrueba : IFachadaAnulacion
    {
        public (int IdVenta, string Motivo)? Recibido { get; private set; }
        public Exception? Error { get; init; }

        public Task AnularVentaAsync(int idVenta, string motivo, CancellationToken cancellationToken = default)
        {
            Recibido = (idVenta, motivo);
            return Error is null ? Task.CompletedTask : Task.FromException(Error);
        }
    }
}
