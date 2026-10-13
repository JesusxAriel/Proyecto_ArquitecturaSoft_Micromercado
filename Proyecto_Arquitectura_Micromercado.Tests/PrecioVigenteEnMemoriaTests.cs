using Proyecto_Arquitectura_Micromercado.Domain.Products;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.EnMemoria;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class PrecioVigenteEnMemoriaTests
{
    private static readonly LookupOption[] Catalogo = [new(1, "Bolsa 1L")];

    private static InMemoryProductRepository CrearRepositorio() =>
        new(new InMemoryPriceHistoryRepository(), Catalogo, Catalogo, Catalogo);

    private static Product NuevoProducto() => new()
    {
        Nombre = "Leche Entera",
        IdEmpaque = 1,
        PrecioVenta = 12.5m,
        PrecioCosto = 8.0m,
        StockMinimo = 5,
        IdCategoria = 1,
        IdProveedor = 1
    };

    [Fact]
    public async Task Un_producto_activo_devuelve_su_precio()
    {
        var repositorio = CrearRepositorio();
        var id = await repositorio.CreateAsync(NuevoProducto());

        Assert.Equal(12.5m, await repositorio.ObtenerPrecioVigenteAsync(id));
    }

    [Fact]
    public async Task Un_producto_inexistente_devuelve_null()
    {
        var repositorio = CrearRepositorio();

        Assert.Null(await repositorio.ObtenerPrecioVigenteAsync(99));
    }

    [Fact]
    public async Task Un_producto_inactivo_devuelve_null()
    {
        var repositorio = CrearRepositorio();
        var id = await repositorio.CreateAsync(NuevoProducto());
        await repositorio.SoftDeleteAsync(id);

        Assert.Null(await repositorio.ObtenerPrecioVigenteAsync(id));
    }
}
