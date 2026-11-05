using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Arquitectura_Micromercado.Application.Categories;
using Proyecto_Arquitectura_Micromercado.Application.Security;
using Proyecto_Arquitectura_Micromercado.Domain.Categories;
using Proyecto_Arquitectura_Micromercado.Dtos.Reportes;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Factories;
using Proyecto_Arquitectura_Micromercado.Pages.Reportes;
using Proyecto_Arquitectura_Micromercado.Servicios.Reportes;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class ResumenCategoriasModelTests
{
    private static async Task<ICategoryService> CrearCategoriasAsync()
    {
        CreatorCategoryRepository creador = new CreatorCategoryRepositoryEnMemoria();
        var servicio = new CategoryService(creador.ObtenerRepositorio());
        await servicio.CreateAsync(new Category { Name = "Lácteos", AisleLocation = "Pasillo 1", Description = "Leche" });
        var inactiva = await servicio.CreateAsync(new Category { Name = "Bebidas", AisleLocation = "Pasillo 2", Description = "Jugos" });
        await servicio.SoftDeleteAsync(inactiva);
        return servicio;
    }

    private static async Task<ResumenCategoriasModel> CrearPaginaAsync(
        ClienteDeReportesFalso cliente,
        FiltrosDeResumen? filtros = null,
        bool consultar = true) =>
        new(cliente, await CrearCategoriasAsync())
        {
            Filtros = filtros ?? new FiltrosDeResumen(),
            Consultar = consultar
        };

    [Fact]
    public async Task Los_indicadores_resumen_monto_unidades_categorias_y_lider_con_su_participacion()
    {
        var cliente = new ClienteDeReportesFalso
        {
            Respuesta = new RespuestaVentasPorCategoriaDto(
                [
                    new FilaVentasPorCategoriaDto(1, "Lácteos", 5, 75m),
                    new FilaVentasPorCategoriaDto(2, "Bebidas", 8, 25m)
                ],
                new TotalesVentasPorCategoriaDto(13, 100m, "Lácteos"),
                new DateTime(2026, 5, 20, 14, 30, 0),
                "admin")
        };
        var pagina = await CrearPaginaAsync(cliente);

        await pagina.OnGetAsync(CancellationToken.None);

        Assert.Equal(["Monto total", "Unidades vendidas", "Categorías", "Categoría líder"], pagina.Indicadores.Select(i => i.Etiqueta));
        Assert.Equal("2", pagina.Indicadores[2].Valor);
        Assert.Equal("Lácteos", pagina.Indicadores[3].Valor);
        Assert.Contains("75", pagina.Indicadores[3].Detalle);
        Assert.Contains("%", pagina.Indicadores[3].Detalle);
    }

    [Fact]
    public async Task Sin_resultado_no_hay_indicadores()
    {
        var pagina = await CrearPaginaAsync(new ClienteDeReportesFalso(), consultar: false);

        await pagina.OnGetAsync(CancellationToken.None);

        Assert.Empty(pagina.Indicadores);
    }

    [Fact]
    public void La_pagina_solo_se_autoriza_al_administrador()
    {
        var atributo = Assert.Single(typeof(ResumenCategoriasModel).GetCustomAttributes<AuthorizeAttribute>());

        Assert.Equal(Roles.Administrador, atributo.Roles);
    }

    [Fact]
    public async Task Sin_consultar_carga_solo_las_categorias_activas_y_no_llama_al_servicio()
    {
        var cliente = new ClienteDeReportesFalso();
        var pagina = await CrearPaginaAsync(cliente, consultar: false);

        await pagina.OnGetAsync(CancellationToken.None);

        Assert.Equal(["Lácteos"], pagina.Categorias.Select(c => c.Text));
        Assert.Equal(0, cliente.Consultas);
        Assert.Null(pagina.Resultado);
    }

    [Fact]
    public async Task Con_filtros_validos_consulta_el_servicio_y_muestra_el_resultado()
    {
        var cliente = new ClienteDeReportesFalso();
        var filtros = new FiltrosDeResumen
        {
            Desde = new DateOnly(2026, 1, 1),
            Hasta = new DateOnly(2026, 1, 31),
            IdCategoria = 1,
            Top = 5
        };
        var pagina = await CrearPaginaAsync(cliente, filtros);

        await pagina.OnGetAsync(CancellationToken.None);

        Assert.Equal(1, cliente.Consultas);
        Assert.Equal(new FiltrosVentasPorCategoriaDto(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), 1, 5), cliente.FiltrosRecibidos);
        Assert.Same(cliente.Respuesta, pagina.Resultado);
        Assert.Null(pagina.MensajeDeError);
    }

    [Fact]
    public async Task Un_rango_invertido_no_llama_al_servicio_y_muestra_el_error_de_validacion()
    {
        var cliente = new ClienteDeReportesFalso();
        var filtros = new FiltrosDeResumen { Desde = new DateOnly(2026, 2, 1), Hasta = new DateOnly(2026, 1, 1) };
        var pagina = await CrearPaginaAsync(cliente, filtros);

        await pagina.OnGetAsync(CancellationToken.None);

        Assert.Equal(0, cliente.Consultas);
        Assert.Null(pagina.Resultado);
        var error = Assert.Single(pagina.ModelState["Filtros.Desde"]!.Errors);
        Assert.Equal("La fecha «Desde» no puede ser posterior a la fecha «Hasta».", error.ErrorMessage);
    }

    [Fact]
    public async Task Un_top_fuera_de_rango_no_llama_al_servicio()
    {
        var cliente = new ClienteDeReportesFalso();
        var pagina = await CrearPaginaAsync(cliente, new FiltrosDeResumen { Top = 0 });
        pagina.ModelState.AddModelError("Filtros.Top", "El valor de «Máximo de categorías» debe ser un número entero entre 1 y 100.");

        await pagina.OnGetAsync(CancellationToken.None);

        Assert.Equal(0, cliente.Consultas);
        Assert.Null(pagina.Resultado);
    }

    [Fact]
    public async Task Una_fecha_con_formato_invalido_se_traduce_a_un_mensaje_en_espanol()
    {
        var cliente = new ClienteDeReportesFalso();
        var pagina = await CrearPaginaAsync(cliente);
        pagina.ModelState.AddModelError("Filtros.Desde", "The value 'abc' is not valid for Desde.");

        await pagina.OnGetAsync(CancellationToken.None);

        Assert.Equal(0, cliente.Consultas);
        var error = Assert.Single(pagina.ModelState["Filtros.Desde"]!.Errors);
        Assert.Equal("Ingrese una fecha válida.", error.ErrorMessage);
    }

    [Fact]
    public async Task Sin_datos_el_resultado_queda_vacio_para_mostrar_el_mensaje_de_sin_resultados()
    {
        var vacio = new RespuestaVentasPorCategoriaDto(
            [], new TotalesVentasPorCategoriaDto(0, 0m, null), new DateTime(2026, 5, 20), "admin");
        var cliente = new ClienteDeReportesFalso { Respuesta = vacio };
        var pagina = await CrearPaginaAsync(cliente);

        await pagina.OnGetAsync(CancellationToken.None);

        Assert.Empty(pagina.Resultado!.Filas);
        Assert.Null(pagina.MensajeDeError);
    }

    [Fact]
    public async Task Un_error_del_servicio_muestra_el_mensaje_y_no_revienta()
    {
        var cliente = new ClienteDeReportesFalso { Error = new ReporteNoDisponibleException("El servicio de reportes no está disponible.") };
        var pagina = await CrearPaginaAsync(cliente);

        await pagina.OnGetAsync(CancellationToken.None);

        Assert.Equal("El servicio de reportes no está disponible.", pagina.MensajeDeError);
        Assert.Null(pagina.Resultado);
    }

    [Theory]
    [InlineData(nameof(ResumenCategoriasModel.OnGetDescargarPdfAsync), FormatoDeDescarga.Pdf)]
    [InlineData(nameof(ResumenCategoriasModel.OnGetDescargarExcelAsync), FormatoDeDescarga.Xlsx)]
    public async Task Las_descargas_devuelven_el_archivo_conservando_los_filtros(string metodo, FormatoDeDescarga formato)
    {
        var cliente = new ClienteDeReportesFalso();
        var pagina = await CrearPaginaAsync(cliente, new FiltrosDeResumen { IdCategoria = 1, Top = 3 });

        var resultado = await InvocarAsync(pagina, metodo);

        var archivo = Assert.IsType<FileContentResult>(resultado);
        Assert.Equal("reporte.pdf", archivo.FileDownloadName);
        Assert.Equal(formato, cliente.FormatoRecibido);
        Assert.Equal(1, cliente.FiltrosRecibidos!.IdCategoria);
        Assert.Equal(3, cliente.FiltrosRecibidos.Top);
    }

    [Fact]
    public async Task Una_descarga_con_filtros_invalidos_no_llama_al_servicio_y_vuelve_a_la_pagina()
    {
        var cliente = new ClienteDeReportesFalso();
        var filtros = new FiltrosDeResumen { Desde = new DateOnly(2026, 2, 1), Hasta = new DateOnly(2026, 1, 1) };
        var pagina = await CrearPaginaAsync(cliente, filtros);

        var resultado = await pagina.OnGetDescargarPdfAsync(CancellationToken.None);

        Assert.IsType<PageResult>(resultado);
        Assert.Equal(0, cliente.Descargas);
        Assert.Equal("La fecha «Desde» no puede ser posterior a la fecha «Hasta».", pagina.MensajeDeError);
    }

    [Fact]
    public async Task Un_error_en_la_descarga_muestra_el_mensaje_y_vuelve_a_la_pagina()
    {
        var cliente = new ClienteDeReportesFalso { Error = new ReporteNoDisponibleException("No tiene permisos para consultar este reporte.") };
        var pagina = await CrearPaginaAsync(cliente);

        var resultado = await pagina.OnGetDescargarExcelAsync(CancellationToken.None);

        Assert.IsType<PageResult>(resultado);
        Assert.Equal("No tiene permisos para consultar este reporte.", pagina.MensajeDeError);
    }

    [Theory]
    [InlineData(100, 100, 100)]
    [InlineData(50, 100, 50)]
    [InlineData(0, 100, 0)]
    [InlineData(1, 1000, 2)]
    public async Task El_ancho_de_la_barra_es_proporcional_al_monto_mayor(int monto, int maximo, int esperado)
    {
        var respuesta = new RespuestaVentasPorCategoriaDto(
            [new FilaVentasPorCategoriaDto(1, "A", 1, maximo), new FilaVentasPorCategoriaDto(2, "B", 1, monto)],
            new TotalesVentasPorCategoriaDto(2, maximo + monto, "A"),
            new DateTime(2026, 5, 20),
            "admin");
        var pagina = await CrearPaginaAsync(new ClienteDeReportesFalso { Respuesta = respuesta });
        await pagina.OnGetAsync(CancellationToken.None);

        Assert.Equal(esperado, pagina.AnchoDeBarra(monto));
    }

    private static Task<IActionResult> InvocarAsync(ResumenCategoriasModel pagina, string metodo) =>
        (Task<IActionResult>)typeof(ResumenCategoriasModel).GetMethod(metodo)!
            .Invoke(pagina, [CancellationToken.None])!;
}
