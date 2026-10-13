using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging.Abstractions;
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

    private static IndexModel CrearPagina(ConsultaPrueba servicio) =>
        new(null!, null!, null!, servicio, null!, NullLogger<IndexModel>.Instance)
        {
            PageContext = new PageContext { HttpContext = new DefaultHttpContext() }
        };

    private sealed class ConsultaPrueba : IVentaConsultaService
    {
        public bool? IncluirAnuladasRecibido { get; private set; }
        public Exception? Error { get; init; }

        public Task<decimal> ObtenerVentasDelDiaAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<VentaResumenDto>> ListarAsync(
            bool incluirAnuladas, CancellationToken cancellationToken = default)
        {
            IncluirAnuladasRecibido = incluirAnuladas;
            if (Error is not null) return Task.FromException<IReadOnlyList<VentaResumenDto>>(Error);

            IReadOnlyList<VentaResumenDto> ventas =
                [new(7, "V-20260615-0007", Fecha, "Cliente Uno", "COMPLETADA", 40m)];
            return Task.FromResult(ventas);
        }

        public Task<VentaDetalleDto?> ObtenerDetalleAsync(
            int idVenta, CancellationToken cancellationToken = default)
        {
            if (Error is not null) return Task.FromException<VentaDetalleDto?>(Error);
            if (idVenta != 7) return Task.FromResult<VentaDetalleDto?>(null);

            return Task.FromResult<VentaDetalleDto?>(new VentaDetalleDto(
                7, "V-20260615-0007", Fecha, "123", "Cliente Uno", "Cajero Uno", "COMPLETADA",
                40m, 50m, 10m, null, null, null,
                [new LineaDetalleVentaDto(4, "Leche", 10m, 40m)]));
        }
    }
}
