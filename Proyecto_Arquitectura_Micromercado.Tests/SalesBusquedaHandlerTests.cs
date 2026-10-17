using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Proyecto_Arquitectura_Micromercado.Application.Common;
using Proyecto_Arquitectura_Micromercado.Application.Customers;
using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Domain.Common;
using Proyecto_Arquitectura_Micromercado.Domain.Products;
using Proyecto_Arquitectura_Micromercado.Pages.Sales;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class SalesBusquedaHandlerTests
{
    private static CancellationToken Cancelado()
    {
        var origen = new CancellationTokenSource();
        origen.Cancel();
        return origen.Token;
    }

    [Fact]
    public async Task La_busqueda_de_productos_devuelve_el_esquema_completo()
    {
        var productos = new ProductosPrueba
        {
            Resultado = new ResultadoBusquedaParaVenta([new ProductoParaVenta(14, "Galletas", "Paquete", 4.5m, 90)], true)
        };
        var pagina = CrearPagina(productos: productos);

        var resultado = Assert.IsType<JsonResult>(await pagina.OnGetBuscarProductosAsync("galle", default));
        var json = JsonSerializer.SerializeToElement(resultado.Value);

        Assert.Null(resultado.StatusCode);
        var producto = Assert.Single(json.GetProperty("productos").EnumerateArray());
        Assert.Equal(14, producto.GetProperty("id").GetInt32());
        Assert.Equal("Paquete", producto.GetProperty("presentacion").GetString());
        Assert.Equal(90, producto.GetProperty("stockDisponible").GetInt32());
        Assert.Equal(1, json.GetProperty("total").GetInt32());
        Assert.True(json.GetProperty("hayMas").GetBoolean());
        Assert.Equal(JsonValueKind.Null, json.GetProperty("mensaje").ValueKind);
        Assert.Equal("no-store", pagina.Response.Headers.CacheControl.ToString());
    }

    [Fact]
    public async Task Si_la_peticion_ya_fue_cancelada_no_se_consulta_y_se_devuelve_vacio_sin_logs()
    {
        var productos = new ProductosPrueba();
        var registro = new RegistroEspia();
        var pagina = CrearPagina(productos: productos, registro: registro);

        var resultado = await pagina.OnGetBuscarProductosAsync("galle", Cancelado());

        Assert.IsType<EmptyResult>(resultado);
        Assert.Equal(0, productos.Llamadas);
        Assert.Empty(registro.Entradas);
    }

    [Fact]
    public async Task Si_la_cancelacion_ocurre_durante_la_consulta_se_devuelve_vacio_sin_500_ni_logs()
    {
        var origen = new CancellationTokenSource();
        var productos = new ProductosPrueba
        {
            Antes = () => origen.Cancel(),
            Error = new TaskCanceledException()
        };
        var registro = new RegistroEspia();
        var pagina = CrearPagina(productos: productos, registro: registro);

        var resultado = await pagina.OnGetBuscarProductosAsync("galle", origen.Token);

        Assert.IsType<EmptyResult>(resultado);
        Assert.Empty(registro.Entradas);
    }

    [Fact]
    public async Task El_token_de_la_peticion_no_se_entrega_a_la_consulta()
    {
        var origen = new CancellationTokenSource();
        var productos = new ProductosPrueba();
        var pagina = CrearPagina(productos: productos);

        await pagina.OnGetBuscarProductosAsync("galle", origen.Token);
        origen.Cancel();

        Assert.NotNull(productos.TokenRecibido);
        Assert.NotEqual(origen.Token, productos.TokenRecibido);
        Assert.False(productos.TokenRecibido!.Value.IsCancellationRequested);
    }

    [Fact]
    public async Task Un_vencimiento_del_tiempo_devuelve_503_con_el_esquema_y_un_aviso()
    {
        var productos = new ProductosPrueba { Error = new TaskCanceledException() };
        var registro = new RegistroEspia();
        var pagina = CrearPagina(productos: productos, registro: registro);

        var resultado = Assert.IsType<JsonResult>(await pagina.OnGetBuscarProductosAsync("galle", default));
        var json = JsonSerializer.SerializeToElement(resultado.Value);

        Assert.Equal(503, resultado.StatusCode);
        Assert.Empty(json.GetProperty("productos").EnumerateArray());
        Assert.Equal(0, json.GetProperty("total").GetInt32());
        Assert.Equal("No se pudo buscar productos. Intente nuevamente.", json.GetProperty("mensaje").GetString());
        Assert.Equal([LogLevel.Warning], registro.Niveles);
    }

    [Fact]
    public async Task Un_error_de_persistencia_devuelve_503_valido_y_se_registra_como_aviso()
    {
        var productos = new ProductosPrueba { Error = new ErrorDePersistenciaException(new Exception("interno")) };
        var registro = new RegistroEspia();
        var pagina = CrearPagina(productos: productos, registro: registro);

        var resultado = Assert.IsType<JsonResult>(await pagina.OnGetBuscarProductosAsync("galle", default));
        var texto = JsonSerializer.Serialize(resultado.Value);

        Assert.Equal(503, resultado.StatusCode);
        Assert.DoesNotContain("interno", texto);
        Assert.Equal([LogLevel.Warning], registro.Niveles);
    }

    [Fact]
    public async Task Una_excepcion_inesperada_devuelve_503_valido_y_se_registra_como_error()
    {
        var productos = new ProductosPrueba { Error = new InvalidOperationException("Información interna") };
        var registro = new RegistroEspia();
        var pagina = CrearPagina(productos: productos, registro: registro);

        var resultado = Assert.IsType<JsonResult>(await pagina.OnGetBuscarProductosAsync("galle", default));
        var json = JsonSerializer.SerializeToElement(resultado.Value);

        Assert.Equal(503, resultado.StatusCode);
        Assert.Equal("No se pudo buscar productos. Intente nuevamente.", json.GetProperty("mensaje").GetString());
        Assert.DoesNotContain("Información interna", JsonSerializer.Serialize(resultado.Value));
        Assert.Equal([LogLevel.Error], registro.Niveles);
    }

    [Fact]
    public async Task La_busqueda_de_clientes_devuelve_la_lista()
    {
        var clientes = new ClientesPrueba { Resultado = [new ClienteConsultaDto(5, "123", "Ana Pérez")] };
        var pagina = CrearPagina(clientes: clientes);

        var resultado = Assert.IsType<JsonResult>(await pagina.OnGetBuscarClientesAsync("ana", default));
        var json = JsonSerializer.SerializeToElement(resultado.Value);

        var cliente = Assert.Single(json.EnumerateArray());
        Assert.Equal(5, cliente.GetProperty("id").GetInt32());
        Assert.Equal("Ana Pérez", cliente.GetProperty("razonSocial").GetString());
    }

    [Fact]
    public async Task La_busqueda_de_clientes_con_la_peticion_cancelada_devuelve_vacio_sin_consultar()
    {
        var clientes = new ClientesPrueba();
        var registro = new RegistroEspia();
        var pagina = CrearPagina(clientes: clientes, registro: registro);

        var resultado = await pagina.OnGetBuscarClientesAsync("ana", Cancelado());

        Assert.IsType<EmptyResult>(resultado);
        Assert.Equal(0, clientes.Llamadas);
        Assert.Empty(registro.Entradas);
    }

    [Fact]
    public async Task La_cancelacion_durante_la_busqueda_de_clientes_no_produce_500_ni_logs()
    {
        var origen = new CancellationTokenSource();
        var clientes = new ClientesPrueba { Antes = () => origen.Cancel(), Error = new OperationCanceledException() };
        var registro = new RegistroEspia();
        var pagina = CrearPagina(clientes: clientes, registro: registro);

        var resultado = await pagina.OnGetBuscarClientesAsync("ana", origen.Token);

        Assert.IsType<EmptyResult>(resultado);
        Assert.Empty(registro.Entradas);
    }

    [Fact]
    public async Task Un_error_en_la_busqueda_de_clientes_devuelve_503_con_mensaje_y_se_registra()
    {
        var clientes = new ClientesPrueba { Error = new ErrorDePersistenciaException(new Exception("interno")) };
        var registro = new RegistroEspia();
        var pagina = CrearPagina(clientes: clientes, registro: registro);

        var resultado = Assert.IsType<JsonResult>(await pagina.OnGetBuscarClientesAsync("ana", default));
        var json = JsonSerializer.SerializeToElement(resultado.Value);

        Assert.Equal(503, resultado.StatusCode);
        Assert.Equal("No se pudo buscar clientes. Intente nuevamente.", json.GetProperty("mensaje").GetString());
        Assert.Equal([LogLevel.Warning], registro.Niveles);
    }

    [Fact]
    public async Task Si_la_peticion_se_cancela_mientras_responde_el_servicio_no_se_serializa_nada()
    {
        var origen = new CancellationTokenSource();
        var productos = new ProductosPrueba
        {
            Antes = () => origen.Cancel(),
            Resultado = new ResultadoBusquedaParaVenta([new ProductoParaVenta(1, "Galletas", "Paquete", 1m, 1)], false)
        };
        var pagina = CrearPagina(productos: productos);

        var resultado = await pagina.OnGetBuscarProductosAsync("galle", origen.Token);

        Assert.IsType<EmptyResult>(resultado);
    }

    [Fact]
    public async Task Si_la_peticion_se_cancela_mientras_responde_la_busqueda_de_clientes_no_se_serializa_nada()
    {
        var origen = new CancellationTokenSource();
        var clientes = new ClientesPrueba
        {
            Antes = () => origen.Cancel(),
            Resultado = [new ClienteConsultaDto(5, "123", "Ana Pérez")]
        };
        var pagina = CrearPagina(clientes: clientes);

        var resultado = await pagina.OnGetBuscarClientesAsync("ana", origen.Token);

        Assert.IsType<EmptyResult>(resultado);
    }

    private static IndexModel CrearPagina(
        ProductosPrueba? productos = null,
        ClientesPrueba? clientes = null,
        RegistroEspia? registro = null) =>
        new(clientes ?? new ClientesPrueba(), productos ?? new ProductosPrueba(), null!, null!, null!, null!,
            registro ?? new RegistroEspia())
        {
            PageContext = new PageContext { HttpContext = new DefaultHttpContext() }
        };

    private sealed class ClientesPrueba : IClienteConsultaService
    {
        public int Llamadas { get; private set; }
        public IReadOnlyList<ClienteConsultaDto> Resultado { get; init; } = [];
        public Exception? Error { get; init; }
        public Action? Antes { get; init; }

        public Task<bool> ExisteNitCiAsync(string nitCi, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<ClienteConsultaDto>> BuscarAsync(string? termino, CancellationToken cancellationToken = default)
        {
            Llamadas++;
            Antes?.Invoke();
            return Error is null
                ? Task.FromResult(Resultado)
                : Task.FromException<IReadOnlyList<ClienteConsultaDto>>(Error);
        }
    }

    private sealed class ProductosPrueba : IProductService
    {
        public int Llamadas { get; private set; }
        public CancellationToken? TokenRecibido { get; private set; }
        public ResultadoBusquedaParaVenta Resultado { get; init; } = ResultadoBusquedaParaVenta.Vacio;
        public Exception? Error { get; init; }
        public Action? Antes { get; init; }

        public Task<ResultadoBusquedaParaVenta> BuscarParaVentaAsync(string? termino, CancellationToken cancellationToken = default)
        {
            Llamadas++;
            TokenRecibido = cancellationToken;
            Antes?.Invoke();
            return Error is null
                ? Task.FromResult(Resultado)
                : Task.FromException<ResultadoBusquedaParaVenta>(Error);
        }

        public Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<int> CreateAsync(Product entity, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> UpdateAsync(Product entity, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> SoftDeleteAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<ProductListItem>> GetAllAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PagedResult<ProductListItem>> GetPagedAsync(int page, int pageSize, string? search, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<LookupOption>> GetPackagingsAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<LookupOption>> GetCategoriesAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<LookupOption>> GetSuppliersAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<ProductPriceHistory>> GetPriceHistoryAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<ProductPriceHistory>> GetPriceHistoryByProductIdAsync(int productId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ProductPriceHistory?> GetPriceHistoryByIdAsync(int historyId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PagedResult<ProductListItem>> GetPagedForSalesAsync(int page, int pageSize, string? search, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> IsDuplicateAsync(string nombre, int idEmpaque, int idExcluido, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
