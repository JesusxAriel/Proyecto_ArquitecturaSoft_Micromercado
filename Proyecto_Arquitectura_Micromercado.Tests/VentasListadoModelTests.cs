using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Arquitectura_Micromercado.Application.Security;
using Proyecto_Arquitectura_Micromercado.Dtos.Reportes;
using Proyecto_Arquitectura_Micromercado.Pages.Reportes;
using Proyecto_Arquitectura_Micromercado.Servicios.Reportes;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class VentasListadoModelTests
{
    private static VentasListadoModel CrearPagina(
        ClienteDeReportesFalso cliente,
        FiltrosDeListado? filtros = null,
        bool consultar = true) =>
        new(cliente)
        {
            Filtros = filtros ?? new FiltrosDeListado(),
            Consultar = consultar
        };

    [Fact]
    public void La_pagina_solo_se_autoriza_al_administrador()
    {
        var atributo = Assert.Single(typeof(VentasListadoModel).GetCustomAttributes<AuthorizeAttribute>());

        Assert.Equal(Roles.Administrador, atributo.Roles);
    }

    [Fact]
    public void La_lista_de_estados_ofrece_completada_y_anulada()
    {
        var pagina = CrearPagina(new ClienteDeReportesFalso());

        Assert.Equal(["Completada", "Anulada"], pagina.Estados.Select(e => e.Text));
        Assert.Equal(["COMPLETADA", "ANULADA"], pagina.Estados.Select(e => e.Value));
    }

    [Fact]
    public async Task Sin_consultar_no_llama_al_servicio()
    {
        var cliente = new ClienteDeReportesFalso();
        var pagina = CrearPagina(cliente, consultar: false);

        await pagina.OnGetAsync(CancellationToken.None);

        Assert.Equal(0, cliente.Consultas);
        Assert.Null(pagina.Resultado);
    }

    [Fact]
    public async Task Con_filtros_validos_consulta_el_servicio_y_muestra_el_resultado()
    {
        var cliente = new ClienteDeReportesFalso();
        var filtros = new FiltrosDeListado
        {
            Desde = new DateOnly(2026, 10, 1),
            Hasta = new DateOnly(2026, 10, 31),
            Estado = "COMPLETADA",
            Cliente = "  Andina  ",
            Tope = 25
        };
        var pagina = CrearPagina(cliente, filtros);

        await pagina.OnGetAsync(CancellationToken.None);

        Assert.Equal(1, cliente.Consultas);
        Assert.Equal(
            new FiltrosListadoVentasDto(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31), "COMPLETADA", "Andina", 25),
            cliente.FiltrosDeListadoRecibidos);
        Assert.Same(cliente.RespuestaDeListado, pagina.Resultado);
        Assert.Null(pagina.MensajeDeError);
    }

    [Fact]
    public async Task Un_estado_vacio_viaja_como_nulo_para_traer_todas_las_ventas()
    {
        var cliente = new ClienteDeReportesFalso();
        var pagina = CrearPagina(cliente, new FiltrosDeListado { Estado = "", Cliente = "   " });

        await pagina.OnGetAsync(CancellationToken.None);

        Assert.Null(cliente.FiltrosDeListadoRecibidos!.Estado);
        Assert.Null(cliente.FiltrosDeListadoRecibidos.Cliente);
    }

    [Fact]
    public async Task Un_rango_invertido_no_llama_al_servicio_y_muestra_el_error_de_validacion()
    {
        var cliente = new ClienteDeReportesFalso();
        var filtros = new FiltrosDeListado { Desde = new DateOnly(2026, 2, 1), Hasta = new DateOnly(2026, 1, 1) };
        var pagina = CrearPagina(cliente, filtros);

        await pagina.OnGetAsync(CancellationToken.None);

        Assert.Equal(0, cliente.Consultas);
        Assert.Null(pagina.Resultado);
        var error = Assert.Single(pagina.ModelState["Filtros.Desde"]!.Errors);
        Assert.Equal("La fecha «Desde» no puede ser posterior a la fecha «Hasta».", error.ErrorMessage);
    }

    [Fact]
    public async Task Un_tope_fuera_de_rango_no_llama_al_servicio()
    {
        var cliente = new ClienteDeReportesFalso();
        var pagina = CrearPagina(cliente, new FiltrosDeListado { Tope = 0 });
        pagina.ModelState.AddModelError(
            "Filtros.Tope",
            "El tope de filas debe ser un número entero entre 1 y 1000.");

        await pagina.OnGetAsync(CancellationToken.None);

        Assert.Equal(0, cliente.Consultas);
        Assert.Null(pagina.Resultado);
    }

    [Fact]
    public async Task Una_fecha_con_formato_invalido_se_traduce_a_un_mensaje_en_espanol()
    {
        var cliente = new ClienteDeReportesFalso();
        var pagina = CrearPagina(cliente);
        pagina.ModelState.AddModelError("Filtros.Hasta", "The value 'abc' is not valid for Hasta.");

        await pagina.OnGetAsync(CancellationToken.None);

        Assert.Equal(0, cliente.Consultas);
        var error = Assert.Single(pagina.ModelState["Filtros.Hasta"]!.Errors);
        Assert.Equal("Ingrese una fecha válida.", error.ErrorMessage);
    }

    [Fact]
    public async Task Sin_datos_el_resultado_queda_vacio_para_mostrar_el_mensaje_de_sin_resultados()
    {
        var vacio = new RespuestaListadoVentasDto(
            [],
            new TotalesListadoVentasDto(0, 0, 0m, 0m),
            new DateTime(2026, 11, 3, 9, 15, 0),
            "admin");
        var cliente = new ClienteDeReportesFalso { RespuestaDeListado = vacio };
        var pagina = CrearPagina(cliente);

        await pagina.OnGetAsync(CancellationToken.None);

        Assert.Empty(pagina.Resultado!.Filas);
        Assert.Null(pagina.MensajeDeError);
    }

    [Fact]
    public async Task Un_error_del_servicio_muestra_el_mensaje_y_no_revienta()
    {
        var cliente = new ClienteDeReportesFalso
        {
            Error = new ReporteNoDisponibleException("El tope de filas debe estar entre 1 y 1000.")
        };
        var pagina = CrearPagina(cliente);

        await pagina.OnGetAsync(CancellationToken.None);

        Assert.Equal("El tope de filas debe estar entre 1 y 1000.", pagina.MensajeDeError);
        Assert.Null(pagina.Resultado);
    }

    [Theory]
    [InlineData(nameof(VentasListadoModel.OnGetDescargarPdfAsync), FormatoDeDescarga.Pdf)]
    [InlineData(nameof(VentasListadoModel.OnGetDescargarExcelAsync), FormatoDeDescarga.Xlsx)]
    public async Task Las_descargas_devuelven_el_archivo_conservando_los_filtros(string metodo, FormatoDeDescarga formato)
    {
        var cliente = new ClienteDeReportesFalso();
        var pagina = CrearPagina(cliente, new FiltrosDeListado { Estado = "ANULADA", Tope = 10 });

        var resultado = await InvocarAsync(pagina, metodo);

        var archivo = Assert.IsType<FileContentResult>(resultado);
        Assert.Equal("listado-de-ventas.pdf", archivo.FileDownloadName);
        Assert.Equal(formato, cliente.FormatoRecibido);
        Assert.Equal("ANULADA", cliente.FiltrosDeListadoRecibidos!.Estado);
        Assert.Equal(10, cliente.FiltrosDeListadoRecibidos.Tope);
    }

    [Fact]
    public async Task Una_descarga_con_filtros_invalidos_no_llama_al_servicio_y_vuelve_a_la_pagina()
    {
        var cliente = new ClienteDeReportesFalso();
        var filtros = new FiltrosDeListado { Desde = new DateOnly(2026, 2, 1), Hasta = new DateOnly(2026, 1, 1) };
        var pagina = CrearPagina(cliente, filtros);

        var resultado = await pagina.OnGetDescargarPdfAsync(CancellationToken.None);

        Assert.IsType<PageResult>(resultado);
        Assert.Equal(0, cliente.Descargas);
        Assert.Equal("La fecha «Desde» no puede ser posterior a la fecha «Hasta».", pagina.MensajeDeError);
    }

    [Fact]
    public async Task Un_error_en_la_descarga_muestra_el_mensaje_y_vuelve_a_la_pagina()
    {
        var cliente = new ClienteDeReportesFalso
        {
            Error = new ReporteNoDisponibleException("No tiene permisos para consultar este reporte.")
        };
        var pagina = CrearPagina(cliente);

        var resultado = await pagina.OnGetDescargarExcelAsync(CancellationToken.None);

        Assert.IsType<PageResult>(resultado);
        Assert.Equal("No tiene permisos para consultar este reporte.", pagina.MensajeDeError);
    }

    [Fact]
    public void Los_parametros_de_consulta_conservan_los_filtros_para_las_descargas()
    {
        var filtros = new FiltrosDeListado
        {
            Desde = new DateOnly(2026, 10, 1),
            Hasta = new DateOnly(2026, 10, 31),
            Estado = "COMPLETADA",
            Cliente = "Andina",
            Tope = 25
        };
        var pagina = CrearPagina(new ClienteDeReportesFalso(), filtros);

        var parametros = pagina.ParametrosDeConsulta;

        Assert.Equal("true", parametros["Consultar"]);
        Assert.Equal("2026-10-01", parametros["Filtros.Desde"]);
        Assert.Equal("2026-10-31", parametros["Filtros.Hasta"]);
        Assert.Equal("COMPLETADA", parametros["Filtros.Estado"]);
        Assert.Equal("Andina", parametros["Filtros.Cliente"]);
        Assert.Equal("25", parametros["Filtros.Tope"]);
    }

    [Theory]
    [InlineData("COMPLETADA", "Completada", "badge bg-success")]
    [InlineData("ANULADA", "Anulada", "badge bg-warning text-dark")]
    public void El_estado_se_muestra_con_su_nombre_y_su_color(string estado, string nombre, string clase)
    {
        Assert.Equal(nombre, VentasListadoModel.NombreDelEstado(estado));
        Assert.Equal(clase, VentasListadoModel.ClaseDelEstado(estado));
    }

    private static Task<IActionResult> InvocarAsync(VentasListadoModel pagina, string metodo) =>
        (Task<IActionResult>)typeof(VentasListadoModel)
            .GetMethod(metodo)!
            .Invoke(pagina, [CancellationToken.None])!;
}
