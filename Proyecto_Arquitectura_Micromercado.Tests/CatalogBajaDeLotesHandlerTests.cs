using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Arquitectura_Micromercado.Application.Common;
using Proyecto_Arquitectura_Micromercado.Application.Stock;
using Proyecto_Arquitectura_Micromercado.Domain.Stock;
using Proyecto_Arquitectura_Micromercado.Pages.Products;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class CatalogBajaDeLotesHandlerTests
{
    [Fact]
    public async Task El_administrador_da_de_baja_y_ve_el_resumen()
    {
        var servicio = new BajaPrueba { Resultado = new ResultadoBajaDeLotes(2, 8) };
        var pagina = CrearPagina(servicio, "Administrador");

        var resultado = await pagina.OnPostDarDeBajaLotesVencidosAsync(default);

        Assert.IsType<RedirectToPageResult>(resultado);
        Assert.Equal(1, servicio.Llamadas);
        Assert.Equal("Se dieron de baja 2 lotes vencidos con 8 unidades en total.", pagina.StatusMessage);
        Assert.Null(pagina.ErrorMessage);
    }

    [Fact]
    public async Task El_mensaje_usa_el_singular_cuando_es_un_lote_con_una_unidad()
    {
        var pagina = CrearPagina(new BajaPrueba { Resultado = new ResultadoBajaDeLotes(1, 1) }, "Administrador");

        await pagina.OnPostDarDeBajaLotesVencidosAsync(default);

        Assert.Equal("Se dio de baja 1 lote vencido con 1 unidad en total.", pagina.StatusMessage);
    }

    [Fact]
    public async Task Sin_lotes_vencidos_informa_que_no_hay_nada_que_dar_de_baja()
    {
        var pagina = CrearPagina(new BajaPrueba { Resultado = new ResultadoBajaDeLotes(0, 0) }, "Administrador");

        await pagina.OnPostDarDeBajaLotesVencidosAsync(default);

        Assert.Equal("No hay lotes vencidos con stock para dar de baja.", pagina.StatusMessage);
    }

    [Fact]
    public async Task El_cajero_recibe_prohibido_y_no_se_llama_al_servicio()
    {
        var servicio = new BajaPrueba();
        var pagina = CrearPagina(servicio, "Cajero");

        var resultado = await pagina.OnPostDarDeBajaLotesVencidosAsync(default);

        Assert.IsType<ForbidResult>(resultado);
        Assert.Equal(0, servicio.Llamadas);
    }

    [Fact]
    public async Task Un_error_de_persistencia_muestra_un_mensaje_generico()
    {
        var servicio = new BajaPrueba { Error = new ErrorDePersistenciaException(new Exception("interno")) };
        var pagina = CrearPagina(servicio, "Administrador");

        await pagina.OnPostDarDeBajaLotesVencidosAsync(default);

        Assert.Equal("No se pudo dar de baja los lotes vencidos. Intente nuevamente.", pagina.ErrorMessage);
        Assert.Null(pagina.StatusMessage);
    }

    private static CatalogModel CrearPagina(BajaPrueba servicio, string rol)
    {
        var identidad = new ClaimsIdentity([new Claim(ClaimTypes.Role, rol)], "prueba");
        return new CatalogModel(null!, servicio)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identidad) }
            }
        };
    }

    private sealed class BajaPrueba : IBajaDeLotesService
    {
        public int Llamadas { get; private set; }
        public ResultadoBajaDeLotes Resultado { get; init; } = new(0, 0);
        public Exception? Error { get; init; }

        public Task<ResultadoBajaDeLotes> DarDeBajaVencidosAsync(CancellationToken cancellationToken = default)
        {
            Llamadas++;
            return Error is null
                ? Task.FromResult(Resultado)
                : Task.FromException<ResultadoBajaDeLotes>(Error);
        }

        public Task<ResultadoBajaDeLotes> DarDeBajaVencidosDelSistemaAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
