using Proyecto_Arquitectura_Micromercado.Infrastructure.Web;
using Proyecto_Arquitectura_Micromercado.Application.Categories;
using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Application.Stock;
using Proyecto_Arquitectura_Micromercado.Domain.Products;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Database;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Factories;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class FactoryMethodTests
{
    private static MySqlUnidadDeTrabajo UnidadDePrueba() =>
        new(DatabaseConnection.GetInstance("Server=localhost;Database=prueba;Uid=prueba;Pwd=prueba;"));

    private static CreatorCategoryRepository[] CreadoresDeCategoria() =>
    [
        new CreatorCategoryRepositoryEnMemoria(),
        new CreatorCategoryRepositoryMySql(UnidadDePrueba(), new UsuarioActualEnMemoria())
    ];

    [Fact]
    public void Un_cliente_usa_el_creador_abstracto_sin_conocer_el_motor()
    {
        foreach (CreatorCategoryRepository creador in CreadoresDeCategoria())
        {
            ICategoryRepository repositorio = creador.ObtenerRepositorio();

            Assert.NotNull(repositorio);
        }
    }

    [Fact]
    public void ObtenerRepositorio_devuelve_siempre_la_misma_instancia()
    {
        foreach (CreatorCategoryRepository creador in CreadoresDeCategoria())
        {
            Assert.Same(creador.ObtenerRepositorio(), creador.ObtenerRepositorio());
        }
    }

    [Fact]
    public void Cada_creador_tiene_su_propia_instancia()
    {
        var primero = new CreatorCategoryRepositoryEnMemoria();
        var segundo = new CreatorCategoryRepositoryEnMemoria();

        Assert.NotSame(primero.ObtenerRepositorio(), segundo.ObtenerRepositorio());
    }

    [Fact]
    public async Task El_creador_de_producto_comparte_la_instancia_de_historial()
    {
        CreatorPriceHistoryRepository creadorHistorial = new CreatorPriceHistoryRepositoryEnMemoria();
        CreatorProductRepository creadorProductos = new CreatorProductRepositoryEnMemoria(creadorHistorial);

        IProductRepository productos = creadorProductos.ObtenerRepositorio();
        IPriceHistoryRepository historial = creadorHistorial.ObtenerRepositorio();

        var producto = new Product
        {
            Nombre = "Leche entera",
            IdEmpaque = 1,
            PrecioVenta = 10m,
            PrecioCosto = 8m,
            StockMinimo = 5,
            IdCategoria = 1,
            IdProveedor = 1
        };

        await productos.CreateAsync(producto);

        producto.PrecioVenta = 12m;
        await productos.UpdateAsync(producto);

        IReadOnlyList<ProductPriceHistory> registros = await historial.GetPriceHistoryAsync();

        Assert.Single(registros);
        Assert.Equal(10m, registros[0].PrecioVentaAnterior);
        Assert.Equal(12m, registros[0].PrecioVentaNuevo);
    }

    [Fact]
    public async Task No_registra_historial_si_los_precios_no_cambian()
    {
        CreatorPriceHistoryRepository creadorHistorial = new CreatorPriceHistoryRepositoryEnMemoria();
        CreatorProductRepository creadorProductos = new CreatorProductRepositoryEnMemoria(creadorHistorial);

        IProductRepository productos = creadorProductos.ObtenerRepositorio();

        var producto = new Product
        {
            Nombre = "Arroz",
            IdEmpaque = 2,
            PrecioVenta = 15m,
            PrecioCosto = 11m,
            IdCategoria = 1,
            IdProveedor = 1
        };

        await productos.CreateAsync(producto);

        producto.StockMinimo = 20;
        await productos.UpdateAsync(producto);

        Assert.Empty(await creadorHistorial.ObtenerRepositorio().GetPriceHistoryAsync());
    }

    [Fact]
    public void El_creador_de_lotes_en_memoria_devuelve_siempre_la_misma_instancia()
    {
        CreatorLoteRepository creador = new CreatorLoteRepositoryEnMemoria();

        ILoteRepository primero = creador.ObtenerRepositorio();

        Assert.Same(primero, creador.ObtenerRepositorio());
    }
}
