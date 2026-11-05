using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PuntoMarket.Ventas.Application.Comprobantes;
using PuntoMarket.Ventas.Application.Customers;
using PuntoMarket.Ventas.Application.Products;

namespace PuntoMarket.Ventas.Tests.Api;

public sealed class ComplementosHttpTests : IAsyncLifetime
{
    private ApiTestHost host = null!;
    private readonly ConsultasComplementariasDoble lecturas = new();
    public async Task InitializeAsync() => host = await ApiTestHost.CrearAsync(configurarServicios: servicios =>
    {
        servicios.AddSingleton<IProductoVentaConsultaRepository>(lecturas);
        servicios.AddSingleton<IClienteVentaConsultaRepository>(lecturas);
        servicios.AddSingleton<IComprobanteRepository>(lecturas);
        servicios.RemoveAll<IClienteCreacionService>();
        servicios.AddSingleton<IClienteCreacionService, ClienteCreacionDoble>();
    });
    public async Task DisposeAsync() => await host.DisposeAsync();

    [Theory]
    [InlineData("Administrador")]
    [InlineData("Cajero")]
    public async Task Crear_cliente_permite_roles_operativos_y_devuelve_cliente(string rol)
    {
        host.Autenticar(rol, "23");
        using var respuesta = await host.Client.PostAsJsonAsync("/api/clientes",
            new { nitCi = "12345", razonSocial = "Cliente Nuevo" });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var cliente = await respuesta.Content.ReadFromJsonAsync<ClienteConsultaDto>();
        Assert.Equal(17, cliente!.Id);
        Assert.Equal("12345", cliente.NitCi);
    }

    [Theory]
    [InlineData("Reportes")]
    [InlineData("Cajero")]
    public async Task Crear_cliente_rechaza_acceso_no_autorizado_o_dato_invalido(string valor)
    {
        host.Autenticar(valor);
        using var respuesta = await host.Client.PostAsJsonAsync("/api/clientes",
            valor == "Cajero" ? new { nitCi = "", razonSocial = "x" } : new { nitCi = "12345", razonSocial = "x" });

        Assert.Equal(valor == "Cajero" ? HttpStatusCode.BadRequest : HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task Crear_cliente_rechaza_espacios_con_error_de_validacion()
    {
        host.Autenticar();
        using var respuesta = await host.Client.PostAsJsonAsync("/api/clientes",
            new { nitCi = " ", razonSocial = " " });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Theory]
    [InlineData("/api/productos?termino=Leche")]
    [InlineData("/api/productos/1")]
    [InlineData("/api/productos/1/precio")]
    [InlineData("/api/productos/1/stock")]
    [InlineData("/api/clientes?termino=123")]
    [InlineData("/api/ventas/7/comprobante")]
    [InlineData("/api/ventas/7/comprobante/html")]
    public async Task Todas_las_rutas_exigen_token_y_rol_permitido(string ruta)
    {
        using var anonima = await host.Client.GetAsync(ruta);
        Assert.Equal(HttpStatusCode.Unauthorized, anonima.StatusCode);
        host.Autenticar("Reportes");
        using var prohibida = await host.Client.GetAsync(ruta);
        Assert.Equal(HttpStatusCode.Forbidden, prohibida.StatusCode);
        host.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", host.Token(debeCambiar: true));
        using var pendiente = await host.Client.GetAsync(ruta);
        Assert.Equal(HttpStatusCode.Forbidden, pendiente.StatusCode);
        Assert.Equal(0, lecturas.Lecturas);
    }

    internal sealed class ClienteCreacionDoble : IClienteCreacionService
    {
        public Task<ClienteConsultaDto> CrearAsync(ClienteCreacionDto cliente, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ClienteConsultaDto(17, cliente.NitCi.Trim(), cliente.RazonSocial.Trim()));
    }

    [Theory]
    [InlineData("Administrador")]
    [InlineData("Cajero")]
    public async Task Productos_devuelven_los_campos_originales_y_excluyen_inactivos(string rol)
    {
        host.Autenticar(rol);
        using var respuesta = await host.Client.GetAsync("/api/productos?termino=%20Leche%20");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("no-store", respuesta.Headers.CacheControl!.ToString());
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, json.GetProperty("total").GetInt32());
        Assert.False(json.GetProperty("hayMas").GetBoolean());
        Assert.Equal(JsonValueKind.Null, json.GetProperty("mensaje").ValueKind);
        var producto = Assert.Single(json.GetProperty("productos").EnumerateArray());
        Assert.Equal(1, producto.GetProperty("id").GetInt32());
        Assert.Equal("Bolsa 1L", producto.GetProperty("presentacion").GetString());
        Assert.Equal(10.50m, producto.GetProperty("precioVenta").GetDecimal());
        Assert.Equal(8, producto.GetProperty("stockDisponible").GetInt32());
    }

    [Theory]
    [InlineData("/api/productos?termino=no-existe", true)]
    [InlineData("/api/productos?termino=a", true)]
    [InlineData("/api/productos", true)]
    [InlineData("/api/clientes?termino=no-existe", false)]
    [InlineData("/api/clientes", false)]
    public async Task Las_busquedas_vacias_son_200_con_colecciones_vacias(string ruta, bool productos)
    {
        host.Autenticar();
        using var respuesta = await host.Client.GetAsync(ruta);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Empty((productos ? json.GetProperty("productos") : json).EnumerateArray());
    }

    [Fact]
    public async Task Detalle_precio_stock_y_cliente_se_consultan_sin_escrituras()
    {
        host.Autenticar();
        var producto = await host.Client.GetFromJsonAsync<ProductoParaVenta>("/api/productos/1");
        Assert.Equal(10.50m, producto!.PrecioVenta);
        Assert.Equal(8, producto.StockDisponible);
        var precio = await host.Client.GetFromJsonAsync<JsonElement>("/api/productos/1/precio");
        Assert.Equal(10.50m, precio.GetProperty("precioVenta").GetDecimal());
        var stock = await host.Client.GetFromJsonAsync<JsonElement>("/api/productos/3/stock");
        Assert.Equal(0, stock.GetProperty("stockDisponible").GetInt32());
        var clientes = await host.Client.GetFromJsonAsync<List<ClienteConsultaDto>>("/api/clientes?termino=123");
        var cliente = Assert.Single(clientes!);
        Assert.Equal(3, cliente.Id);
        Assert.Equal("12345", cliente.NitCi);
        Assert.Equal("Cliente Uno", cliente.RazonSocial);
        Assert.Empty(host.Estado.Operaciones); // Ningún puerto de escritura ni unidad transaccional invocados.
        Assert.Empty(host.Estado.Ventas.Cabeceras);
    }

    [Theory]
    [InlineData("/api/productos/2")]
    [InlineData("/api/productos/2/precio")]
    [InlineData("/api/productos/2/stock")]
    [InlineData("/api/productos/99")]
    [InlineData("/api/ventas/99/comprobante")]
    [InlineData("/api/ventas/99/comprobante/html")]
    public async Task Entidades_inactivas_o_inexistentes_devuelven_404(string ruta)
    {
        host.Autenticar();
        using var respuesta = await host.Client.GetAsync(ruta);
        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType!.MediaType);
    }

    [Theory]
    [InlineData("/api/productos/0")]
    [InlineData("/api/productos/-1/precio")]
    [InlineData("/api/productos/0/stock")]
    [InlineData("/api/ventas/0/comprobante")]
    [InlineData("/api/ventas/0/comprobante/html")]
    public async Task Un_identificador_invalido_se_rechaza_antes_de_leer(string ruta)
    {
        host.Autenticar();
        using var respuesta = await host.Client.GetAsync(ruta);
        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal(0, lecturas.Lecturas);
    }

    [Theory]
    [InlineData("productos")]
    [InlineData("clientes")]
    public async Task Un_filtro_excesivo_se_rechaza_sin_consultar(string recurso)
    {
        host.Autenticar();
        using var respuesta = await host.Client.GetAsync($"/api/{recurso}?termino={new string('a', 151)}");
        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal(0, lecturas.Lecturas);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Comprobantes_JSON_y_HTML_conservan_venta_cajero_y_estado_sin_modificarla(bool anulada)
    {
        host.Autenticar("Administrador", "99");
        if (anulada) lecturas.Venta = lecturas.Venta! with { Estado = "ANULADA" };
        var original = lecturas.Venta;
        var comprobante = await host.Client.GetFromJsonAsync<ComprobanteDto>("/api/ventas/7/comprobante");
        Assert.NotNull(comprobante);
        Assert.Equal(31.50m, comprobante.Total);
        Assert.Equal(3, Assert.Single(comprobante.Lineas).Cantidad);
        Assert.Equal("cajero-origen", comprobante.UsuarioGenerador);
        Assert.Equal(original!.FechaVenta, comprobante.FechaGeneracion);
        Assert.Equal(anulada, comprobante.EstaAnulada);
        using var html = await host.Client.GetAsync("/api/ventas/7/comprobante/html");
        Assert.Equal(HttpStatusCode.OK, html.StatusCode);
        Assert.Equal("text/html", html.Content.Headers.ContentType!.MediaType);
        Assert.Contains("no-store", html.Headers.CacheControl!.ToString());
        var texto = await html.Content.ReadAsStringAsync();
        Assert.Contains("COMPROBANTE DE VENTA", texto);
        Assert.Contains("window.print()", texto);
        Assert.Contains("cajero-origen", texto);
        Assert.Contains("&lt;script&gt;cliente&lt;/script&gt;", texto);
        Assert.DoesNotContain("<script>cliente", texto);
        Assert.Equal(anulada, texto.Contains("role=\"alert\">ANULADA</p>"));
        Assert.Equal(original, lecturas.Venta);
        Assert.Empty(host.Estado.Operaciones);
    }
}
