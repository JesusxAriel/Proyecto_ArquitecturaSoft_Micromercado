using Proyecto_Arquitectura_Micromercado.Application.Categories;
using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Domain.Products;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Database;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Factories;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;

namespace Proyecto_Arquitectura_Micromercado.Tests;

// Pruebas del patron Factory Method en si mismo, no de una entidad en particular.
// Ninguna abre una conexion: construir un Creador y pedirle su repositorio no toca la
// base de datos, asi que el motor MySQL tambien se puede verificar aca.
public class FactoryMethodTests
{
    // Unidad de trabajo de mentira: nunca se abre una conexion, solo se necesita para
    // construir el Creador Concreto de MySQL. Construir el Creador y pedirle su
    // repositorio no toca la base de datos.
    private static MySqlUnidadDeTrabajo UnidadDePrueba() =>
        new(DatabaseConnection.GetInstance("Server=localhost;Database=prueba;Uid=prueba;Pwd=prueba;"));

    // Los dos Creadores Concretos de la misma jerarquia, declarados con el tipo ABSTRACTO.
    // Que esta linea compile es la demostracion de que el polimorfismo del patron existe:
    // antes de la correccion no habia ningun tipo capaz de contener a los dos.
    private static CreatorCategoryRepository[] CreadoresDeCategoria() =>
    [
        new CreatorCategoryRepositoryEnMemoria(),
        new CreatorCategoryRepositoryMySql(UnidadDePrueba())
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

    // Regresion del defecto corregido: el Creador de Producto reutiliza el repositorio de
    // historial de su fabrica en vez de crear uno paralelo. Si volviera a llamar a
    // CrearRepositorio(), el producto escribiria en una instancia distinta de la que
    // consulta el resto de la aplicacion y este historial vendria vacio.
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
}
