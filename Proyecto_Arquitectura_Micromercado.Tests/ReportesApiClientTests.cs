using System.Net;
using System.Net.Http.Headers;
using Proyecto_Arquitectura_Micromercado.Dtos.Reportes;
using Proyecto_Arquitectura_Micromercado.Servicios.Reportes;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class ReportesApiClientTests
{
    private const string JsonDeEjemplo = """
        {"filas":[{"idCategoria":5,"categoria":"Snacks y Galletas","unidadesVendidas":91,"montoVendido":432.00},
        {"idCategoria":1,"categoria":"Lácteos y Derivados","unidadesVendidas":2,"montoVendido":13.00}],
        "totales":{"totalUnidades":93,"totalMonto":445.00,"categoriaLider":"Snacks y Galletas"},
        "filtros":{"desde":null,"hasta":null,"idCategoria":null,"top":null},
        "generadoEn":"2026-11-02T23:24:40","generadoPor":"admin"}
        """;

    private static readonly FiltrosVentasPorCategoriaDto SinFiltros = new(null, null, null, null);

    private static ReportesApiClient CrearCliente(
        ManejadorHttpFalso manejador,
        ITokenDeAcceso? token = null,
        bool conUrlBase = true)
    {
        var http = new HttpClient(manejador);
        if (conUrlBase)
            http.BaseAddress = new Uri("http://reportes.prueba/");

        return new ReportesApiClient(http, token ?? new TokenDeAccesoFalso());
    }

    [Fact]
    public async Task Sin_filtros_solo_envia_el_formato_json()
    {
        var manejador = new ManejadorHttpFalso(HttpStatusCode.OK, JsonDeEjemplo);

        await CrearCliente(manejador).ObtenerVentasPorCategoriaAsync(SinFiltros, CancellationToken.None);

        Assert.Equal("/api/reportes/ventas-por-categoria?formato=json", manejador.Peticion!.RequestUri!.PathAndQuery);
    }

    [Fact]
    public async Task Con_todos_los_filtros_arma_la_cadena_de_consulta_en_formato_invariante()
    {
        var manejador = new ManejadorHttpFalso(HttpStatusCode.OK, JsonDeEjemplo);
        var filtros = new FiltrosVentasPorCategoriaDto(new DateOnly(2026, 1, 5), new DateOnly(2026, 2, 28), 3, 10);

        await CrearCliente(manejador).ObtenerVentasPorCategoriaAsync(filtros, CancellationToken.None);

        Assert.Equal(
            "/api/reportes/ventas-por-categoria?desde=2026-01-05&hasta=2026-02-28&idCategoria=3&top=10&formato=json",
            manejador.Peticion!.RequestUri!.PathAndQuery);
    }

    [Fact]
    public async Task Con_filtros_parciales_omite_los_que_no_se_indicaron()
    {
        var manejador = new ManejadorHttpFalso(HttpStatusCode.OK, JsonDeEjemplo);
        var filtros = new FiltrosVentasPorCategoriaDto(null, new DateOnly(2026, 2, 28), null, 5);

        await CrearCliente(manejador).ObtenerVentasPorCategoriaAsync(filtros, CancellationToken.None);

        Assert.Equal(
            "/api/reportes/ventas-por-categoria?hasta=2026-02-28&top=5&formato=json",
            manejador.Peticion!.RequestUri!.PathAndQuery);
    }

    [Fact]
    public async Task Agrega_el_encabezado_bearer_con_el_token_de_acceso()
    {
        var manejador = new ManejadorHttpFalso(HttpStatusCode.OK, JsonDeEjemplo);

        await CrearCliente(manejador, new TokenDeAccesoFalso("abc.def.ghi"))
            .ObtenerVentasPorCategoriaAsync(SinFiltros, CancellationToken.None);

        Assert.Equal(new AuthenticationHeaderValue("Bearer", "abc.def.ghi"), manejador.Peticion!.Headers.Authorization);
    }

    [Fact]
    public async Task Deserializa_el_json_del_servicio()
    {
        var manejador = new ManejadorHttpFalso(HttpStatusCode.OK, JsonDeEjemplo);

        var respuesta = await CrearCliente(manejador).ObtenerVentasPorCategoriaAsync(SinFiltros, CancellationToken.None);

        Assert.Equal(2, respuesta.Filas.Count);
        Assert.Equal("Snacks y Galletas", respuesta.Filas[0].Categoria);
        Assert.Equal(91, respuesta.Filas[0].UnidadesVendidas);
        Assert.Equal(432.00m, respuesta.Filas[0].MontoVendido);
        Assert.Equal(445.00m, respuesta.Totales.TotalMonto);
        Assert.Equal("Snacks y Galletas", respuesta.Totales.CategoriaLider);
        Assert.Equal("admin", respuesta.GeneradoPor);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "Su sesión con el servicio de reportes no es válida o venció. Inicie sesión nuevamente.")]
    [InlineData(HttpStatusCode.Forbidden, "No tiene permisos para consultar este reporte.")]
    [InlineData(HttpStatusCode.BadRequest, "Los filtros indicados no son válidos. Revise las fechas y el valor de «Top».")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "El servicio de reportes no está disponible. Intente nuevamente en unos minutos.")]
    [InlineData(HttpStatusCode.InternalServerError, "El servicio de reportes no pudo generar el reporte. Intente nuevamente.")]
    public async Task Traduce_los_errores_http_a_mensajes_claros_sin_filtrar_detalles(HttpStatusCode estado, string mensaje)
    {
        var manejador = new ManejadorHttpFalso(estado, "detalle interno: Access denied for user 'root'");

        var error = await Assert.ThrowsAsync<ReporteNoDisponibleException>(
            () => CrearCliente(manejador).ObtenerVentasPorCategoriaAsync(SinFiltros, CancellationToken.None));

        Assert.Equal(mensaje, error.Message);
        Assert.DoesNotContain("root", error.Message);
    }

    [Fact]
    public async Task Si_el_servicio_esta_caido_lanza_el_mensaje_de_no_disponible()
    {
        var manejador = new ManejadorHttpFalso(_ => throw new HttpRequestException("conexión rechazada"));

        var error = await Assert.ThrowsAsync<ReporteNoDisponibleException>(
            () => CrearCliente(manejador).ObtenerVentasPorCategoriaAsync(SinFiltros, CancellationToken.None));

        Assert.Equal("El servicio de reportes no está disponible. Intente nuevamente en unos minutos.", error.Message);
    }

    [Fact]
    public async Task Si_el_servicio_no_responde_a_tiempo_lanza_el_mensaje_de_no_disponible()
    {
        var manejador = new ManejadorHttpFalso(_ => throw new TaskCanceledException("tiempo agotado"));

        var error = await Assert.ThrowsAsync<ReporteNoDisponibleException>(
            () => CrearCliente(manejador).ObtenerVentasPorCategoriaAsync(SinFiltros, CancellationToken.None));

        Assert.Equal("El servicio de reportes no está disponible. Intente nuevamente en unos minutos.", error.Message);
    }

    [Fact]
    public async Task La_cancelacion_del_usuario_no_se_traduce()
    {
        using var origen = new CancellationTokenSource();
        await origen.CancelAsync();
        var manejador = new ManejadorHttpFalso(_ => throw new TaskCanceledException());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => CrearCliente(manejador).ObtenerVentasPorCategoriaAsync(SinFiltros, origen.Token));
    }

    [Fact]
    public async Task Un_json_invalido_se_traduce_al_mensaje_de_generacion()
    {
        var manejador = new ManejadorHttpFalso(HttpStatusCode.OK, "esto no es json");

        var error = await Assert.ThrowsAsync<ReporteNoDisponibleException>(
            () => CrearCliente(manejador).ObtenerVentasPorCategoriaAsync(SinFiltros, CancellationToken.None));

        Assert.Equal("El servicio de reportes no pudo generar el reporte. Intente nuevamente.", error.Message);
    }

    [Fact]
    public async Task Sin_url_base_configurada_lanza_el_mensaje_de_no_configurado()
    {
        var manejador = new ManejadorHttpFalso(HttpStatusCode.OK, JsonDeEjemplo);

        var error = await Assert.ThrowsAsync<ReporteNoDisponibleException>(
            () => CrearCliente(manejador, conUrlBase: false)
                .ObtenerVentasPorCategoriaAsync(SinFiltros, CancellationToken.None));

        Assert.Equal("El servicio de reportes no está configurado.", error.Message);
        Assert.Null(manejador.Peticion);
    }

    [Fact]
    public async Task Sin_token_de_acceso_pide_iniciar_sesion_y_no_llama_al_servicio()
    {
        var manejador = new ManejadorHttpFalso(HttpStatusCode.OK, JsonDeEjemplo);

        var error = await Assert.ThrowsAsync<ReporteNoDisponibleException>(
            () => CrearCliente(manejador, new TokenDeAccesoFalso(null))
                .ObtenerVentasPorCategoriaAsync(SinFiltros, CancellationToken.None));

        Assert.Contains("Inicie sesión nuevamente", error.Message);
        Assert.Null(manejador.Peticion);
    }

    [Fact]
    public async Task La_descarga_devuelve_los_bytes_el_tipo_y_el_nombre_del_archivo()
    {
        var manejador = new ManejadorHttpFalso(_ =>
        {
            var respuesta = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([37, 80, 68, 70]) };
            respuesta.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            respuesta.Content.Headers.ContentDisposition =
                new ContentDispositionHeaderValue("attachment") { FileName = "ventas-por-categoria-20260520.pdf" };
            return respuesta;
        });

        var archivo = await CrearCliente(manejador)
            .DescargarVentasPorCategoriaAsync(SinFiltros, FormatoDeDescarga.Pdf, CancellationToken.None);

        Assert.Equal([37, 80, 68, 70], archivo.Contenido);
        Assert.Equal("application/pdf", archivo.TipoDeContenido);
        Assert.Equal("ventas-por-categoria-20260520.pdf", archivo.NombreDeArchivo);
        Assert.EndsWith("formato=pdf", manejador.Peticion!.RequestUri!.Query);
    }

    [Fact]
    public async Task La_descarga_usa_un_nombre_por_defecto_si_el_servicio_no_lo_envia()
    {
        var manejador = new ManejadorHttpFalso(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent([80, 75])
        });

        var archivo = await CrearCliente(manejador)
            .DescargarVentasPorCategoriaAsync(SinFiltros, FormatoDeDescarga.Xlsx, CancellationToken.None);

        Assert.Equal("ventas-por-categoria.xlsx", archivo.NombreDeArchivo);
        Assert.EndsWith("formato=xlsx", manejador.Peticion!.RequestUri!.Query);
    }

    [Fact]
    public async Task La_descarga_tambien_traduce_los_errores()
    {
        var manejador = new ManejadorHttpFalso(HttpStatusCode.Forbidden);

        var error = await Assert.ThrowsAsync<ReporteNoDisponibleException>(
            () => CrearCliente(manejador)
                .DescargarVentasPorCategoriaAsync(SinFiltros, FormatoDeDescarga.Pdf, CancellationToken.None));

        Assert.Equal("No tiene permisos para consultar este reporte.", error.Message);
    }
}
