using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Domain.Products;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.EnMemoria;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class BusquedaParaVentaTests
{
    private static readonly LookupOption[] Empaques = [new(1, "Bolsa 1L"), new(2, "Paquete")];

    private static (IProductService servicio, IProductRepository repositorio) Crear()
    {
        var historial = new InMemoryPriceHistoryRepository();
        var repositorio = new InMemoryProductRepository(historial, Empaques, Empaques, Empaques);
        return (new ProductService(repositorio, historial), repositorio);
    }

    private static Product Producto(string nombre, int idEmpaque = 1, decimal precio = 5m) => new()
    {
        Nombre = nombre,
        IdEmpaque = idEmpaque,
        PrecioVenta = precio,
        PrecioCosto = 1m,
        StockMinimo = 1,
        IdCategoria = 1,
        IdProveedor = 1
    };

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("g")]
    [InlineData(" g ")]
    public async Task Un_termino_vacio_o_de_un_solo_caracter_no_consulta_y_devuelve_vacio(string? termino)
    {
        var (servicio, repositorio) = Crear();
        await repositorio.CreateAsync(Producto("Galletas Moka"));

        var resultado = await servicio.BuscarParaVentaAsync(termino);

        Assert.Empty(resultado.Productos);
        Assert.False(resultado.HayMas);
    }

    [Fact]
    public async Task Con_dos_caracteres_busca_por_nombre_y_devuelve_los_datos_para_la_venta()
    {
        var (servicio, repositorio) = Crear();
        await repositorio.CreateAsync(Producto("Galletas Moka", precio: 4.5m));
        await repositorio.CreateAsync(Producto("Leche Entera"));

        var resultado = await servicio.BuscarParaVentaAsync("  GA ");

        var producto = Assert.Single(resultado.Productos);
        Assert.Equal("Galletas Moka", producto.Nombre);
        Assert.Equal("Bolsa 1L", producto.Presentacion);
        Assert.Equal(4.5m, producto.PrecioVenta);
        Assert.Equal(0, producto.StockDisponible);
    }

    [Fact]
    public async Task Tambien_busca_por_presentacion_y_ordena_por_nombre()
    {
        var (servicio, repositorio) = Crear();
        await repositorio.CreateAsync(Producto("Zanahoria", idEmpaque: 2));
        await repositorio.CreateAsync(Producto("Arroz", idEmpaque: 2));
        await repositorio.CreateAsync(Producto("Leche", idEmpaque: 1));

        var resultado = await servicio.BuscarParaVentaAsync("paquete");

        Assert.Equal(["Arroz", "Zanahoria"], resultado.Productos.Select(p => p.Nombre).ToArray());
    }

    [Fact]
    public async Task Devuelve_como_maximo_diez_resultados_e_indica_que_hay_mas()
    {
        var (servicio, repositorio) = Crear();
        for (var i = 1; i <= 12; i++)
        {
            await repositorio.CreateAsync(Producto($"Galleta {i:00}"));
        }

        var resultado = await servicio.BuscarParaVentaAsync("galleta");

        Assert.Equal(10, resultado.Productos.Count);
        Assert.True(resultado.HayMas);
    }

    [Fact]
    public async Task Con_exactamente_diez_coincidencias_no_indica_que_hay_mas()
    {
        var (servicio, repositorio) = Crear();
        for (var i = 1; i <= 10; i++)
        {
            await repositorio.CreateAsync(Producto($"Galleta {i:00}"));
        }

        var resultado = await servicio.BuscarParaVentaAsync("galleta");

        Assert.Equal(10, resultado.Productos.Count);
        Assert.False(resultado.HayMas);
    }

    [Fact]
    public async Task Ignora_los_productos_dados_de_baja()
    {
        var (servicio, repositorio) = Crear();
        var id = await repositorio.CreateAsync(Producto("Galletas Moka"));
        await repositorio.SoftDeleteAsync(id);

        var resultado = await servicio.BuscarParaVentaAsync("galle");

        Assert.Empty(resultado.Productos);
    }
}
