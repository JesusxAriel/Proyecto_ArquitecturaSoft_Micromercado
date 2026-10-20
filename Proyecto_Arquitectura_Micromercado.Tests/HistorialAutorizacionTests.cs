using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Arquitectura_Micromercado.Application.Facades;
using Proyecto_Arquitectura_Micromercado.Application.Sales;
using Proyecto_Arquitectura_Micromercado.Application.Security;
using Proyecto_Arquitectura_Micromercado.Domain.Sales;
using Proyecto_Arquitectura_Micromercado.Domain.Stock;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.EnMemoria;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Web;
using Proyecto_Arquitectura_Micromercado.Pages.Sales;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class HistorialAutorizacionTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 6, 15, 10, 0, 0, TimeSpan.Zero);

    private sealed class Escenario
    {
        public UnidadDeTrabajoEspia Unidad { get; } = new();
        public VentaRepositoryEnMemoria Ventas { get; } = new();
        public InMemoryLoteRepository Lotes { get; } = new();
        public FachadaVenta Venta { get; }
        public FachadaAnulacion Anulacion { get; }

        public Escenario()
        {
            var reloj = new RelojFijo(Ahora);
            var stock = new FachadaActualizacionStockProducto(Lotes, reloj, new UnidadDeTrabajoEspia(), new UsuarioActualEnMemoria());
            var precios = new PrecioVigenteDoble(new Dictionary<int, decimal> { [1] = 10m });
            Venta = new FachadaVenta(
                new UnidadDeTrabajoEspia(), Ventas, precios, stock, new UsuarioActualEnMemoria(id: 7), reloj,
                new DetectorDeInterbloqueoDoble(), new ClientesDoble(3));
            Anulacion = new FachadaAnulacion(Unidad, Ventas, stock, new UsuarioActualEnMemoria(id: 9));
        }

        public async Task<int> RegistrarVenta()
        {
            Lotes.Agregar(1, new LoteDisponible(10, 20, new DateOnly(2026, 8, 1)));
            var resultado = await Venta.RegistrarVentaAsync(new NuevaVentaDto(3, [new LineaVentaDto(1, 5)]));
            return resultado.IdVenta;
        }

        public async Task<int> Disponible()
        {
            var vigentes = await Lotes.ObtenerVigentesParaDescuentoAsync(1);
            return vigentes.Single(l => l.IdLote == 10).CantidadDisponible;
        }

        public string Estado(int idVenta) => Ventas.Cabeceras.Single(c => c.Id == idVenta).Estado;
    }

    private static HistorialModel CrearPagina(Escenario escenario, string rol, IVentaConsultaService? consulta = null)
    {
        var identidad = new ClaimsIdentity([new Claim(ClaimTypes.Role, rol)], "prueba");
        return new HistorialModel(consulta ?? new ConsultaVacia(), escenario.Anulacion, new RegistroEspia())
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identidad) }
            }
        };
    }

    private static HistorialModel.AnularVentaInput Entrada(int idVenta) => new()
    {
        IdVenta = idVenta,
        Motivo = "Error de digitación"
    };

    [Fact]
    public void La_pagina_exige_el_rol_administrador_o_cajero()
    {
        var atributo = typeof(HistorialModel).GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(atributo);
        Assert.Equal(Roles.AdministradorYCajero, atributo.Roles);
    }

    [Theory]
    [InlineData("Administrador", true)]
    [InlineData("Cajero", false)]
    public void Solo_el_administrador_puede_anular(string rol, bool esperado)
    {
        var pagina = CrearPagina(new Escenario(), rol);

        Assert.Equal(esperado, pagina.PuedeAnular);
    }

    [Theory]
    [InlineData("Administrador")]
    [InlineData("Cajero")]
    public async Task Ambos_roles_ven_el_historial_y_el_detalle(string rol)
    {
        var pagina = CrearPagina(new Escenario(), rol, new ConsultaConUnaVenta());

        var historial = Assert.IsType<JsonResult>(await pagina.OnGetHistorialAsync(true, default));
        var detalle = Assert.IsType<JsonResult>(await pagina.OnGetDetalleAsync(7, default));

        Assert.Equal(1, JsonSerializer.SerializeToElement(historial.Value).GetProperty("ventas").GetArrayLength());
        Assert.Equal("V-20260615-0007", JsonSerializer.SerializeToElement(detalle.Value).GetProperty("nroComprobante").GetString());
    }

    [Fact]
    public async Task El_cajero_que_intenta_anular_recibe_403_y_la_venta_sigue_completada()
    {
        var e = new Escenario();
        var idVenta = await e.RegistrarVenta();
        var pagina = CrearPagina(e, "Cajero");

        var resultado = Assert.IsType<JsonResult>(await pagina.OnPostAnularVentaAsync(Entrada(idVenta), default));

        Assert.Equal(403, resultado.StatusCode);
        Assert.Equal(EstadoVenta.Completada, e.Estado(idVenta));
        Assert.Equal(15, await e.Disponible());
        Assert.Equal(0, e.Unidad.Iniciadas);
    }

    [Fact]
    public async Task El_administrador_anula_la_venta_por_la_fachada_con_su_unidad_de_trabajo()
    {
        var e = new Escenario();
        var idVenta = await e.RegistrarVenta();
        var pagina = CrearPagina(e, "Administrador");

        var resultado = Assert.IsType<JsonResult>(await pagina.OnPostAnularVentaAsync(Entrada(idVenta), default));

        Assert.True(JsonSerializer.SerializeToElement(resultado.Value).GetProperty("exito").GetBoolean());
        Assert.Equal(EstadoVenta.Anulada, e.Estado(idVenta));
        Assert.Equal(20, await e.Disponible());
        Assert.Equal(1, e.Unidad.Iniciadas);
        Assert.Equal(1, e.Unidad.Confirmadas);
    }

    [Fact]
    public async Task Una_venta_anulada_no_se_puede_anular_otra_vez()
    {
        var e = new Escenario();
        var idVenta = await e.RegistrarVenta();
        var pagina = CrearPagina(e, "Administrador");
        await pagina.OnPostAnularVentaAsync(Entrada(idVenta), default);

        var resultado = Assert.IsType<JsonResult>(await pagina.OnPostAnularVentaAsync(Entrada(idVenta), default));
        var json = JsonSerializer.SerializeToElement(resultado.Value);

        Assert.Equal(400, resultado.StatusCode);
        Assert.Equal("La venta no existe o ya fue anulada.", json.GetProperty("mensaje").GetString());
        Assert.Equal(20, await e.Disponible());
    }

    [Fact]
    public void El_menu_ofrece_el_historial_a_todos_los_roles_y_ya_no_dice_proximamente()
    {
        var layout = File.ReadAllText(Path.Combine(RaizDelProyecto(), "Pages", "Shared", "_Layout.cshtml"));
        var inicio = layout.IndexOf("Caja y Ventas", StringComparison.Ordinal);
        var menu = layout[inicio..];

        var enlace = menu.IndexOf("asp-page=\"/Sales/Historial\"", StringComparison.Ordinal);
        var primeraCondicion = menu.IndexOf("@if (", StringComparison.Ordinal);

        Assert.True(enlace > 0);
        Assert.True(enlace < primeraCondicion);
        Assert.Contains("Historial de ventas", menu[enlace..(enlace + 160)]);
        Assert.DoesNotMatch(@"Historial de Ventas\s*<span[^>]*>Próximamente", layout);
        Assert.DoesNotContain("Arqueo", layout, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Distribución por Pasillos", layout);
        Assert.Contains("Productos por Proveedor", layout);
    }

    [Fact]
    public void El_modal_y_el_boton_de_anular_solo_se_dibujan_para_el_administrador()
    {
        var vista = File.ReadAllText(Path.Combine(RaizDelProyecto(), "Pages", "Sales", "Historial.cshtml"));
        var script = File.ReadAllText(Path.Combine(RaizDelProyecto(), "wwwroot", "js", "historial.js"));

        var condicion = vista.IndexOf("@if (Model.PuedeAnular)", StringComparison.Ordinal);
        var modal = vista.IndexOf("id=\"anularVentaModal\"", StringComparison.Ordinal);

        Assert.True(condicion >= 0 && condicion < modal);
        Assert.Contains("data-puede-anular=\"@(Model.PuedeAnular ? \"true\" : \"false\")\"", vista);
        Assert.Contains("VentasReglas.puedeMostrarAnular(puedeAnular, venta.estado)", script);
    }

    [Fact]
    public void La_pantalla_de_registro_ya_no_tiene_historial_detalle_ni_anulacion()
    {
        var metodos = typeof(IndexModel).GetMethods().Select(m => m.Name).ToList();
        var script = File.ReadAllText(Path.Combine(RaizDelProyecto(), "wwwroot", "js", "ventas.js"));
        var vista = File.ReadAllText(Path.Combine(RaizDelProyecto(), "Pages", "Sales", "Index.cshtml"));

        Assert.DoesNotContain("OnGetHistorialAsync", metodos);
        Assert.DoesNotContain("OnGetDetalleAsync", metodos);
        Assert.DoesNotContain("OnPostAnularVentaAsync", metodos);
        Assert.DoesNotContain("historialVentasSeccion", script);
        Assert.DoesNotContain("anularVentaModal", vista);
        Assert.DoesNotContain("detalleVentaModal", vista);
        Assert.DoesNotContain("historialVentasSeccion", vista);
    }

    [Fact]
    public void El_historial_y_el_detalle_no_muestran_efectivo_ni_cambio()
    {
        var vista = File.ReadAllText(Path.Combine(RaizDelProyecto(), "Pages", "Sales", "Historial.cshtml"));
        var script = File.ReadAllText(Path.Combine(RaizDelProyecto(), "wwwroot", "js", "historial.js"));

        foreach (var contenido in new[] { vista, script })
        {
            Assert.DoesNotContain("efectivo", contenido, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("cambio", contenido, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string RaizDelProyecto()
    {
        var directorio = new DirectoryInfo(AppContext.BaseDirectory);

        while (directorio is not null && !File.Exists(Path.Combine(directorio.FullName, "Proyecto_Arquitectura_Micromercado.slnx")))
        {
            directorio = directorio.Parent;
        }

        return Path.Combine(
            directorio?.FullName ?? throw new DirectoryNotFoundException("No se encontro la raiz del repositorio."),
            "Proyecto_Arquitectura_Micromercado");
    }

    private class ConsultaVacia : IVentaConsultaService
    {
        public Task InvalidarHistorialAsync(
    CancellationToken cancellationToken = default)
{
    return Task.CompletedTask;
}
        public virtual Task<decimal> ObtenerVentasDelDiaAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(0m);

        public virtual Task<IReadOnlyList<VentaResumenDto>> ListarAsync(bool incluirAnuladas, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<VentaResumenDto>>([]);

        public virtual Task<VentaDetalleDto?> ObtenerDetalleAsync(int idVenta, CancellationToken cancellationToken = default) =>
            Task.FromResult<VentaDetalleDto?>(null);
    }

    private sealed class ConsultaConUnaVenta : ConsultaVacia
    {
        public override Task<IReadOnlyList<VentaResumenDto>> ListarAsync(bool incluirAnuladas, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<VentaResumenDto>>(
                [new(7, "V-20260615-0007", new DateTime(2026, 6, 15, 10, 30, 0), "Cliente Uno", "COMPLETADA", 40m)]);

        public override Task<VentaDetalleDto?> ObtenerDetalleAsync(int idVenta, CancellationToken cancellationToken = default) =>
            Task.FromResult<VentaDetalleDto?>(new VentaDetalleDto(
                7, "V-20260615-0007", new DateTime(2026, 6, 15, 10, 30, 0), "123", "Cliente Uno", "cajero1",
                "COMPLETADA", 40m, null, null, null, []));
    }
}
