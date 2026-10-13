using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
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
        var resultado = await page.OnPostRegistrarVentaAsync(Entrada(), default);
        var json = JsonSerializer.SerializeToElement(resultado.Value);

        Assert.True(json.GetProperty("exito").GetBoolean());
        Assert.Equal(73, json.GetProperty("idVenta").GetInt32());
        Assert.Equal(19.75m, json.GetProperty("total").GetDecimal());
        Assert.Equal(4, fachada.Recibido!.IdCliente);
        Assert.Equal(25m, fachada.Recibido.EfectivoPagado);
        Assert.Equal(new LineaVentaDto(8, 2), Assert.Single(fachada.Recibido.Lineas));
    }

    [Fact]
    public async Task BindingInvalidoNoInvocaLaFachada()
    {
        var fachada = new FachadaPrueba();
        var page = CrearPagina(fachada);
        page.ModelState.AddModelError("venta.Lineas[0].Cantidad", "Valor inválido");

        var resultado = await page.OnPostRegistrarVentaAsync(Entrada(), default);

        Assert.Equal(400, resultado.StatusCode);
        Assert.Null(fachada.Recibido);
    }

    [Fact]
    public async Task ErrorDeNegocioDevuelveMensajeLegible()
    {
        var page = CrearPagina(new FachadaPrueba { Error = new InvalidOperationException("Efectivo insuficiente.") });

        var resultado = await page.OnPostRegistrarVentaAsync(Entrada(), default);
        var json = JsonSerializer.SerializeToElement(resultado.Value);

        Assert.Equal(400, resultado.StatusCode);
        Assert.False(json.GetProperty("exito").GetBoolean());
        Assert.Equal("Efectivo insuficiente.", json.GetProperty("mensaje").GetString());
    }

    [Fact]
    public async Task ErrorInesperadoNoExponeDetallesInternos()
    {
        var page = CrearPagina(new FachadaPrueba { Error = new Exception("Información interna de conexión") });

        var resultado = await page.OnPostRegistrarVentaAsync(Entrada(), default);

        Assert.Equal(503, resultado.StatusCode);
        Assert.DoesNotContain("Información interna", JsonSerializer.Serialize(resultado.Value));
    }

    private static IndexModel CrearPagina(FachadaPrueba fachada) =>
        new(null!, null!, fachada, null!, NullLogger<IndexModel>.Instance);

    private static IndexModel.RegistrarVentaInput Entrada() => new()
    {
        IdCliente = 4,
        EfectivoPagado = 25m,
        Lineas = [new() { IdProducto = 8, Cantidad = 2 }]
    };

    private sealed class FachadaPrueba : IFachadaVenta
    {
        public NuevaVentaDto? Recibido { get; private set; }
        public Exception? Error { get; init; }

        public Task<ResultadoVentaDto> RegistrarVentaAsync(NuevaVentaDto dto, CancellationToken cancellationToken = default)
        {
            Recibido = dto;
            return Error is null
                ? Task.FromResult(new ResultadoVentaDto(73, 19.75m))
                : Task.FromException<ResultadoVentaDto>(Error);
        }
    }
}
