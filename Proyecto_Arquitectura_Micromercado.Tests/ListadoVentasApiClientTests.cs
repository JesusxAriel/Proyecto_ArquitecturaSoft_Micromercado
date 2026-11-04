using System.Net;
using Proyecto_Arquitectura_Micromercado.Dtos.Reportes;
using Proyecto_Arquitectura_Micromercado.Servicios.Reportes;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class ListadoVentasApiClientTests
{
    private const string JsonDeEjemplo = """
        {"filas":[{"idVenta":118,"comprobante":"F-000118","fecha":"2026-10-30T19:42:00","nitCi":"4587963",
        "razonSocial":"Distribuidora La Paz S.R.L.","cajero":"ventas01","estado":"COMPLETADA","monto":1842.50}],
        "totales":{"ventasCompletadas":1,"ventasAnuladas":0,"montoTotalDeCompletadas":1842.50,"promedioPorVenta":1842.50},
        "generadoEn":"2026-11-03T09:15:00","generadoPor":"admin"}
        """;

    private static readonly FiltrosListadoVentasDto SinFiltros = new(null, null, null, null, null);

    private static ReportesApiClient CrearCliente(ManejadorHttpFalso manejador, ITokenDeAcceso? token = null)
    {
        var http = new HttpClient(manejador) { BaseAddress = new Uri("http://reportes.prueba/") };

        return new ReportesApiClient(http, token ?? new TokenDeAccesoFalso());
    }

    [Fact]
    public async Task Sin_filtros_solo_envia_el_formato_json()
    {
        var manejador = new ManejadorHttpFalso(HttpStatusCode.OK, JsonDeEjemplo);

        await CrearCliente(manejador).ObtenerListadoDeVentasAsync(SinFiltros, CancellationToken.None);

        Assert.Equal("/api/reportes/ventas?formato=json", manejador.Peticion!.RequestUri!.PathAndQuery);
    }

    [Fact]
    public async Task Con_todos_los_filtros_arma_la_cadena_de_consulta_en_formato_invariante()
    {
        var manejador = new ManejadorHttpFalso(HttpStatusCode.OK, JsonDeEjemplo);
        var filtros = new FiltrosListadoVentasDto(
            new DateOnly(2026, 10, 1),
            new DateOnly(2026, 10, 31),
            "COMPLETADA",
            "Andina",
            25);

        await CrearCliente(manejador).ObtenerListadoDeVentasAsync(filtros, CancellationToken.None);

        Assert.Equal(
            "/api/reportes/ventas?desde=2026-10-01&hasta=2026-10-31&estado=COMPLETADA&cliente=Andina&tope=25&formato=json",
            manejador.Peticion!.RequestUri!.PathAndQuery);
    }

    [Fact]
    public async Task El_cliente_con_caracteres_especiales_se_escapa_en_la_consulta()
    {
        var manejador = new ManejadorHttpFalso(HttpStatusCode.OK, JsonDeEjemplo);
        var filtros = new FiltrosListadoVentasDto(null, null, null, "Doña Elvira & Hijos", null);

        await CrearCliente(manejador).ObtenerListadoDeVentasAsync(filtros, CancellationToken.None);

        Assert.Contains("cliente=Do%C3%B1a%20Elvira%20%26%20Hijos", manejador.Peticion!.RequestUri!.Query);
    }

    [Fact]
    public async Task Agrega_el_encabezado_bearer_con_el_token_de_acceso()
    {
        var manejador = new ManejadorHttpFalso(HttpStatusCode.OK, JsonDeEjemplo);

        await CrearCliente(manejador, new TokenDeAccesoFalso("token-de-ale"))
            .ObtenerListadoDeVentasAsync(SinFiltros, CancellationToken.None);

        Assert.Equal("Bearer", manejador.Peticion!.Headers.Authorization!.Scheme);
        Assert.Equal("token-de-ale", manejador.Peticion.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task Lee_las_filas_y_los_sumariados_del_json()
    {
        var manejador = new ManejadorHttpFalso(HttpStatusCode.OK, JsonDeEjemplo);

        var respuesta = await CrearCliente(manejador).ObtenerListadoDeVentasAsync(SinFiltros, CancellationToken.None);

        var fila = Assert.Single(respuesta.Filas);
        Assert.Equal("F-000118", fila.Comprobante);
        Assert.Equal("Distribuidora La Paz S.R.L.", fila.RazonSocial);
        Assert.Equal("COMPLETADA", fila.Estado);
        Assert.Equal(1842.50m, fila.Monto);
        Assert.Equal(1, respuesta.Totales.VentasCompletadas);
        Assert.Equal(1842.50m, respuesta.Totales.PromedioPorVenta);
        Assert.Equal("admin", respuesta.GeneradoPor);
    }

    [Fact]
    public async Task Un_400_de_la_api_muestra_el_mensaje_que_envio_el_servicio()
    {
        var problema = """
            {"title":"Bad Request","status":400,"detail":"El tope de filas debe estar entre 1 y 1000."}
            """;
        var manejador = new ManejadorHttpFalso(HttpStatusCode.BadRequest, problema);

        var excepcion = await Assert.ThrowsAsync<ReporteNoDisponibleException>(
            () => CrearCliente(manejador).ObtenerListadoDeVentasAsync(SinFiltros, CancellationToken.None));

        Assert.Equal("El tope de filas debe estar entre 1 y 1000.", excepcion.Message);
    }

    [Fact]
    public async Task Un_400_sin_detalle_muestra_un_mensaje_propio_en_espanol()
    {
        var manejador = new ManejadorHttpFalso(HttpStatusCode.BadRequest, "no es json");

        var excepcion = await Assert.ThrowsAsync<ReporteNoDisponibleException>(
            () => CrearCliente(manejador).ObtenerListadoDeVentasAsync(SinFiltros, CancellationToken.None));

        Assert.Equal(
            "Los filtros indicados no son válidos. Revise las fechas, el estado, el cliente y el tope de filas.",
            excepcion.Message);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "Su sesión con el servicio de reportes no es válida o venció. Inicie sesión nuevamente.")]
    [InlineData(HttpStatusCode.Forbidden, "No tiene permisos para consultar este reporte.")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "El servicio de reportes no está disponible. Intente nuevamente en unos minutos.")]
    public async Task Los_demas_errores_muestran_el_mensaje_que_corresponde(HttpStatusCode estado, string esperado)
    {
        var manejador = new ManejadorHttpFalso(estado);

        var excepcion = await Assert.ThrowsAsync<ReporteNoDisponibleException>(
            () => CrearCliente(manejador).ObtenerListadoDeVentasAsync(SinFiltros, CancellationToken.None));

        Assert.Equal(esperado, excepcion.Message);
    }

    [Fact]
    public async Task Si_el_servicio_no_responde_muestra_un_mensaje_amable()
    {
        var manejador = new ManejadorHttpFalso(_ => throw new HttpRequestException("sin conexión"));

        var excepcion = await Assert.ThrowsAsync<ReporteNoDisponibleException>(
            () => CrearCliente(manejador).ObtenerListadoDeVentasAsync(SinFiltros, CancellationToken.None));

        Assert.Equal(
            "El servicio de reportes no está disponible. Intente nuevamente en unos minutos.",
            excepcion.Message);
    }

    [Fact]
    public async Task Sin_token_de_acceso_pide_iniciar_sesion_nuevamente()
    {
        var manejador = new ManejadorHttpFalso(HttpStatusCode.OK, JsonDeEjemplo);

        var excepcion = await Assert.ThrowsAsync<ReporteNoDisponibleException>(
            () => CrearCliente(manejador, new TokenDeAccesoFalso(null))
                .ObtenerListadoDeVentasAsync(SinFiltros, CancellationToken.None));

        Assert.Equal(
            "Su sesión con el servicio de reportes no es válida o venció. Inicie sesión nuevamente.",
            excepcion.Message);
    }

    [Theory]
    [InlineData(FormatoDeDescarga.Pdf, "pdf", "application/pdf")]
    [InlineData(FormatoDeDescarga.Xlsx, "xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public async Task La_descarga_pide_el_formato_y_conserva_el_nombre_del_archivo(
        FormatoDeDescarga formato,
        string extension,
        string tipo)
    {
        var manejador = new ManejadorHttpFalso(_ => RespuestaDeArchivo(tipo, $"listado-de-ventas-20261103-091500.{extension}"));

        var archivo = await CrearCliente(manejador)
            .DescargarListadoDeVentasAsync(SinFiltros, formato, CancellationToken.None);

        Assert.Contains($"formato={extension}", manejador.Peticion!.RequestUri!.Query);
        Assert.Equal(tipo, archivo.TipoDeContenido);
        Assert.Equal($"listado-de-ventas-20261103-091500.{extension}", archivo.NombreDeArchivo);
    }

    [Fact]
    public async Task Una_descarga_sin_nombre_de_archivo_usa_el_nombre_por_defecto()
    {
        var manejador = new ManejadorHttpFalso(_ => RespuestaDeArchivo("application/pdf", null));

        var archivo = await CrearCliente(manejador)
            .DescargarListadoDeVentasAsync(SinFiltros, FormatoDeDescarga.Pdf, CancellationToken.None);

        Assert.Equal("listado-de-ventas.pdf", archivo.NombreDeArchivo);
    }

    private static HttpResponseMessage RespuestaDeArchivo(string tipo, string? nombre)
    {
        var contenido = new ByteArrayContent([1, 2, 3]);
        contenido.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(tipo);
        if (nombre is not null)
            contenido.Headers.ContentDisposition =
                new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment") { FileName = nombre };

        return new HttpResponseMessage(HttpStatusCode.OK) { Content = contenido };
    }
}
